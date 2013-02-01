﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ObjectSerialization
{
    internal static class TypeSerializerBuilder
    {
        public static Type[] PredefinedTypes = new[]
            {
                typeof (bool), typeof (byte), typeof (sbyte), typeof (char), typeof (ushort), typeof (short), typeof (int),
                typeof (uint), typeof (long), typeof (ulong), typeof (float), typeof (double), typeof (decimal),
                typeof(string)
            };
    }
    internal static class TypeSerializerBuilder<T>
    {
        #region Type: Context

        class Context
        {
            public readonly List<Expression> ReadExpressions = new List<Expression>();

            public readonly ParameterExpression ReadResultObject = Expression.Variable(typeof(T), "o");
            public readonly ParameterExpression ReaderObject = Expression.Parameter(typeof(BinaryReader), "r");
            public readonly List<Expression> WriteExpressions = new List<Expression>();
            public readonly ParameterExpression WriteObject = Expression.Parameter(typeof(object), "o");
            public readonly ParameterExpression WriterObject = Expression.Parameter(typeof(BinaryWriter), "w");


            public Context()
            {
                ReadExpressions.Add(Expression.Assign(ReadResultObject, Expression.New(typeof(T))));
            }

            public Func<BinaryReader, object> GetDeserializeFn()
            {
                LabelTarget label = Expression.Label(typeof(object));
                List<Expression> expressions = ReadExpressions.ToList();
                expressions.Add(Expression.Return(label, ReadResultObject, typeof(object)));
                expressions.Add(Expression.Label(label, Expression.New(typeof(T))));
                BlockExpression body = Expression.Block(new[] { ReadResultObject }, expressions);
                return Expression.Lambda<Func<BinaryReader, object>>(body, ReaderObject).Compile();
            }

            public Action<BinaryWriter, object> GetSerializeFn()
            {
                return Expression.Lambda<Action<BinaryWriter, object>>(Expression.Block(WriteExpressions), WriterObject, WriteObject).Compile();
            }
        }

        #endregion

        public static Func<BinaryReader, object> DeserializeFn { get; private set; }
        public static Action<BinaryWriter, object> SerializeFn { get; private set; }

        static TypeSerializerBuilder()
        {
            Build();
        }

        private static void Build()
        {
            var ctx = new Context();
            IOrderedEnumerable<PropertyInfo> properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                 .Where(p => p.GetGetMethod() != null && p.GetSetMethod() != null)
                                 .OrderBy(p => p.Name);

            foreach (PropertyInfo property in properties)
            {
                Type propType = property.PropertyType;
                if (TypeSerializerBuilder.PredefinedTypes.Contains(propType))
                    BuildPredefined(property, ctx);
                else if (propType.IsArray)
                    BuildArray(property);
                else if (IsCollection(propType))
                    BuildCollection(property);
                else if (propType == typeof(object) || propType.IsAbstract || propType.IsInterface)
                    BuildPolymorphic(property);
                else if (propType.IsValueType)
                    BuildValueType(property);
                else
                    BuildClass(property, ctx);
            }

            SerializeFn = ctx.GetSerializeFn();
            DeserializeFn = ctx.GetDeserializeFn();

        }

        private static void BuildArray(PropertyInfo property)
        {
            throw new NotImplementedException();
        }

        private static void BuildClass(PropertyInfo property, Context ctx)
        {
            WriteClass(property, ctx);
            ReadClass(property, ctx);
        }

        private static void ReadClass(PropertyInfo property, Context ctx)
        {
            /*BinaryReader r;
            T o;
            if (r.ReadBoolean())
                o.Prop = TypeSerializer.GetDeserializer(typeof(T)).Invoke(r);*/

            var readBool = GetReadExpression("ReadBoolean", ctx.ReaderObject);
            var deserializer = GetDeserializer(property);
            var readValue = Expression.TypeAs(Expression.Call(deserializer, "Invoke", null, ctx.ReaderObject), property.PropertyType);

            var setProperty = GetSetPropertyValue(ctx.ReadResultObject, property, readValue);
            ctx.ReadExpressions.Add(Expression.IfThen(readBool, setProperty));
        }

        private static void WriteClass(PropertyInfo property, Context ctx)
        {
            /*BinaryWriter w;
            object o;
            w.Write(((T)o).Prop!=null);
            if(((T)o).Prop!=null)
            TypeSerializer.GetSerializer(type).Invoke(w, ((T)o).Prop);*/

            var propertyValue = GetGetPropertyValue(ctx.WriteObject, property);
            var isNullExpression = CheckNotNull(propertyValue, property.PropertyType);
            var getSerializer = GetSerializer(property);
            var writeExpression = Expression.Call(getSerializer, "Invoke", null, ctx.WriterObject, propertyValue);

            ctx.WriteExpressions.Add(GetWriteExpression(isNullExpression, ctx.WriterObject));
            ctx.WriteExpressions.Add(Expression.IfThen(isNullExpression, writeExpression));
        }

        private static MethodCallExpression GetSerializer(PropertyInfo property)
        {
            return Expression.Call(typeof(TypeSerializer), "GetSerializer", null, Expression.Constant(property.PropertyType));
        }

        private static MethodCallExpression GetDeserializer(PropertyInfo property)
        {
            return Expression.Call(typeof(TypeSerializer), "GetDeserializer", null, Expression.Constant(property.PropertyType));
        }

        private static Expression CheckNotNull(Expression value, Type propertyType)
        {
            return Expression.ReferenceNotEqual(value, Expression.Constant(null, propertyType));
        }


        private static void BuildCollection(PropertyInfo property)
        {
            throw new NotImplementedException();
        }

        private static void BuildPolymorphic(PropertyInfo property)
        {
            throw new NotImplementedException();
        }

        private static void BuildPredefined(PropertyInfo property, Context ctx)
        {
            /*BinaryWriter w;   
            object o;
            w.Write(((T)o).Prop);*/
            ctx.WriteExpressions.Add(GetWriteExpression(GetGetPropertyValue(ctx.WriteObject, property), ctx.WriterObject));

            /*T o;
            BinaryReader r;
            o.Prop = r.ReadString();*/
            ctx.ReadExpressions.Add(GetSetPropertyValue(ctx.ReadResultObject, property, GetReadExpression("Read" + property.PropertyType.Name, ctx.ReaderObject)));
        }

        private static void BuildValueType(PropertyInfo property)
        {
            throw new NotImplementedException();
        }

        private static Expression GetGetPropertyValue(ParameterExpression instance, PropertyInfo property)
        {
            var castedInstance = Expression.TypeAs(instance, property.DeclaringType);
            return Expression.Property(castedInstance, property);
        }

        private static Expression GetReadExpression(string method, ParameterExpression reader)
        {
            return Expression.Call(reader, method, new Type[0]);
        }

        private static Expression GetSetPropertyValue(Expression instance, PropertyInfo property, Expression valueExpression)
        {
            return Expression.Call(instance, property.GetSetMethod(), valueExpression);
        }

        private static Expression GetWriteExpression(Expression valueExpression, ParameterExpression writer)
        {
            return Expression.Call(writer, "Write", null, valueExpression);
        }

        private static bool HasAddMethod(Type propType, Type itemType)
        {
            return propType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new[] { itemType }, null) != null;
        }

        private static bool IsCollection(Type propType)
        {
            Type itemType;
            return IsEnumerable(propType, out itemType) && HasAddMethod(propType, itemType);
        }

        private static bool IsEnumerable(Type type, out Type itemType)
        {
            itemType = null;
            Type def = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (def != null)
                itemType = def.GetGenericArguments()[0];
            return def != null;
        }
    }
}