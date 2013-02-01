﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ObjectSerialization
{
    internal class CollectionTypeSerializer : BaseTypeSerializer, ISerializer
    {
        private static bool HasAddMethod(Type propType, Type itemType)
        {
            return propType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new[] { itemType }, null) != null;
        }

        private static bool IsEnumerable(Type type, out Type itemType)
        {
            itemType = null;
            Type def = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (def != null)
                itemType = def.GetGenericArguments()[0];
            return def != null;
        }

        public bool IsSupported(Type type)
        {
            Type itemType;
            return IsEnumerable(type, out itemType) && HasAddMethod(type, itemType);
        }

        public Expression Write(Expression writerObject, Expression value, Type propertyType)
        {
            throw new NotImplementedException();
        }

        public Expression Read(Expression readerObject, Type expectedValueType)
        {
            throw new NotImplementedException();
        }
    }
}