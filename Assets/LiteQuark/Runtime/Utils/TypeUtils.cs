using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace LiteQuark.Runtime
{
    public static class TypeUtils
    {
        public static Type GetTypeWithAssembly(string assemblyFullName, string typeName)
        {
            var assemblyList = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblyList)
            {
                if (assembly.FullName == assemblyFullName)
                {
                    return assembly.GetType(typeName);
                }
            }

            return null;
        }
        
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
        
        public static IList CreateGenericList(Type elementType, object[] array)
        {
            var typeList = typeof(List<>);
            var genericType = typeList.MakeGenericType(elementType);
            var result = Activator.CreateInstance(genericType) as IList;
            foreach (var item in array)
            {
                result.Add(item);
            }
            return result;
        }
        
        public static T GetAttribute<T>(Type type, object[] attrs) where T : Attribute
        {
            T result = null;
            if (attrs != null)
            {
                result = (T)Array.Find(attrs, t => t is T);
            }

            if (result == null)
            {
                result = type?.GetCustomAttribute<T>();
            }
            return result;
        }
        
        public static object[] ToArray(this IList list)
        {
            var result = new object[list.Count];
            for (var index = 0; index < list.Count; ++index)
            {
                result[index] = list[index];
            }
            return result;
        }

        public static IList ToArray(this IList list, Type type)
        {
            var result = (IList)Array.CreateInstance(type, list.Count);
            for (var index = 0; index < result.Count; ++index)
            {
                result[index] = list[index];
            }
            return result;
        }
        
        public static T[] CloneObjectArray<T>(T[] array)
        {
            if (array == null)
            {
                return null;
            }
            
            var result = new T[array.Length];

            for (var index = 0; index < array.Length; ++index)
            {
                if (array[index] is ICloneable clone)
                {
                    result[index] = (T)clone.Clone();
                }
                else
                {
                    result[index] = array[index];
                }
            }

            return result;
        }

        public static T[] CloneDataArray<T>(T[] array) where T : ICloneable
        {
            if (array == null)
            {
                return null;
            }
            
            var result = new T[array.Length];

            for (var index = 0; index < array.Length; ++index)
            {
                result[index] = (T)array[index].Clone();
            }

            return result;
        }
    }
}