﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using ObjectSerialization.Builders.Types;

namespace ObjectSerialization.Builders
{
    internal class ClassMembersSerializerBuilder<T> : SerializerBuilder
    {
        private static Func<BinaryReader, object> _deserializeFn;
        private static Action<BinaryWriter, object> _serializeFn;

        public static Func<BinaryReader, object> DeserializeFn
        {
            get
            {
                if (_deserializeFn == null)
                    Build();
                return _deserializeFn;
            }
        }

        public static Action<BinaryWriter, object> SerializeFn
        {
            get
            {
                if (_serializeFn == null)
                    Build();
                return _serializeFn;
            }
        }

        public static IEnumerable<FieldInfo> GetFields(Type type)
        {
            if (type == null)
                return Enumerable.Empty<FieldInfo>();

            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Concat(GetFields(type.BaseType));
        }

        private static void Build()
        {
            var ctx = new BuildContext<T>(Expression.Variable(typeof(T), "o"));

            IOrderedEnumerable<FieldInfo> fields = GetFields(typeof(T))
                .Where(f => f.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
                .OrderBy(f => f.Name);

            foreach (FieldInfo field in fields)
                BuildFieldSerializer(field, ctx);

            _serializeFn = ctx.GetSerializeFn();
            _deserializeFn = ctx.GetDeserializeFn();
        }

        private static void BuildFieldSerializer(FieldInfo field, BuildContext<T> ctx)
        {
            if (field.IsInitOnly)
                throw new SerializationException(string.Format("Unable to serialize readonly field {0} in type {1}. Please mark it with NonSerialized attribute or remove readonly modifier.", field.Name, typeof(T).FullName));

            ISerializer serializer = Serializers.First(s => s.IsSupported(field.FieldType));
            ctx.AddWriteExpression(serializer.Write(ctx.WriterObject, GetFieldValue(ctx.WriteObject, field), field.FieldType));
            ctx.AddReadExpression(SetFieldValue(ctx.ReadResultObject, field, serializer.Read(ctx.ReaderObject, field.FieldType)));
        }

        private static Expression SetFieldValue(Expression instance, FieldInfo field, Expression valueExpression)
        {
            return Expression.Assign(Expression.Field(instance, field), valueExpression);
        }

        private static Expression GetFieldValue(Expression instance, FieldInfo field)
        {
            return Expression.Field(instance, field);
        }
    }
}