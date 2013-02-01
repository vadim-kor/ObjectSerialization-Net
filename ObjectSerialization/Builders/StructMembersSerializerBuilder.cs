﻿using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ObjectSerialization.Builders.Types;

namespace ObjectSerialization.Builders
{
    internal class StructMembersSerializerBuilder<T> : SerializerBuilder
    {
        public static Func<BinaryReader, object> DeserializeFn { get; private set; }
        public static Action<BinaryWriter, object> SerializeFn { get; private set; }

        static StructMembersSerializerBuilder()
        {
            Build();
        }

        private static void Build()
        {
            var ctx = new BuildContext<T>(Expression.New(typeof(T)));

            IOrderedEnumerable<FieldInfo> fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(p => p.Name);

            foreach (FieldInfo field in fields)
                BuildFieldSerializer(field, ctx);

            SerializeFn = ctx.GetSerializeFn();
            DeserializeFn = ctx.GetDeserializeFn();

        }

        private static void BuildFieldSerializer(FieldInfo field, BuildContext<T> ctx)
        {
            ISerializer serializer = Serializers.First(s => s.IsSupported(field.FieldType));
            ctx.AddWriteExpression(serializer.Write(ctx.WriterObject, GetFieldAccess(ctx.WriteObject, field), field.FieldType));
            ctx.AddReadExpression(GetSetFieldValue(ctx.ReadResultObject, field, serializer.Read(ctx.ReaderObject, field.FieldType)));
        }

        private static Expression GetFieldAccess(Expression instance, FieldInfo field)
        {
            return Expression.Field(instance, field);
        }

        private static Expression GetSetFieldValue(Expression instance, FieldInfo field, Expression valueExpression)
        {
            return Expression.Assign(Expression.Field(instance, field), valueExpression);
        }
    }
}