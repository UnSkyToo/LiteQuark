using System;
using System.Collections.Generic;

namespace LiteBattle.Runtime
{
    public static class LiteTypeSystem
    {
        public static bool Equal(object a, object b)
        {
            if (a == null && b == null)
            {
                return true;
            }
            
            if (a == null || b == null)
            {
                return false;
            }

            return a.Equals(b);
        }
        
        public static bool IsListType(Type type)
        {
            return type.IsGenericType && type.GenericTypeArguments.Length == 1;
        }
        
        public static Type GetElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.GenericTypeArguments.Length > 0)
            {
                return type.GenericTypeArguments[0];
            }

            return null;
        }
        
        public static object CreateInstance(Type type)
        {
            if (type.IsArray)
            {
                return Array.CreateInstance(GetElementType(type), 0);
            }

            return type == typeof(string) ? string.Empty : Activator.CreateInstance(type);
        }

        public static object CreateInstance(Type type, int count)
        {
            return Array.CreateInstance(type, count);
        }

        public static T CreateInstance<T>()
        {
            return (T) CreateInstance(typeof(T));
        }
    }
}