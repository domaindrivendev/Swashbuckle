﻿using System;
using System.Collections.Generic;
using System.Linq;
using Swashbuckle.Models;

namespace Swashbuckle
{
    public static class TypeMappingExtensions
    {
        public static string ToSwaggerType(this Type type)
        {
            return type.ToSwaggerType(null);
        }

        public static string ToSwaggerType(this Type type, IDictionary<string,string> customTypeMappings)
        {
            TypeCategory category;
            Type containedType;
            return type.ToSwaggerType(out category, out containedType, customTypeMappings);
        }

        internal static string ToSwaggerType(this Type type, out TypeCategory category, out Type containedType, IDictionary<string,string> customTypeMappings = null)
        {
            if (type == null)
            {
                category = TypeCategory.Unkown;
                containedType = null;
                return null;
            }

            var primitiveTypeMap = new Dictionary<string,string> {
                {"Byte", "byte"},
                {"Boolean", "boolean"},
                {"Int32", "int"},
                {"Int64", "long"},
                {"Single", "float"},
                {"Double", "double"},
                {"Decimal", "double"},
                {"String", "string"},
                {"DateTime", "date"}
            };
            if (customTypeMappings != null)
                primitiveTypeMap = primitiveTypeMap.Concat(customTypeMappings).ToDictionary(m => m.Key, m => m.Value);

            if (primitiveTypeMap.ContainsKey(type.Name))
            {
                category = TypeCategory.Primitive;
                containedType = null;
                return primitiveTypeMap[type.Name];
            }

            Type innerTypeOfNullable;
            if (type.IsNullableType(out innerTypeOfNullable))
            {
                return innerTypeOfNullable.ToSwaggerType(out category, out containedType);
            }

            if (type.IsEnum)
            {
                category = TypeCategory.Primitive;
                containedType = null;
                return "string";
            }

            var enumerable = type.AsGenericType(typeof(IEnumerable<>));
            if (enumerable != null)
            {
                category = TypeCategory.Container;
                containedType = enumerable.GetGenericArguments().First();
                return String.Format("List[{0}]", containedType.ToSwaggerType(customTypeMappings));
            }

            category = TypeCategory.Complex;
            containedType = null;
            return type.Name;
        }
    }

    internal enum TypeCategory
    {
        Unkown,
        Primitive,
        Container,
        Complex
    }
}