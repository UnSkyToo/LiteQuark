using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace LiteQuark.Runtime
{
    public static class TypeUtils
    {
        private static List<Assembly> AssemblyList_ = new List<Assembly>();

        static TypeUtils()
        {
            AddAssembly(typeof(TypeUtils).Assembly);
            AddAssembly(Assembly.GetExecutingAssembly());
            AddAssembly(Assembly.GetEntryAssembly());
        }

        public static void AddAssembly(Assembly assembly, int index = -1)
        {
            if (assembly == null)
            {
                return;
            }
            
            if (AssemblyList_.Contains(assembly))
            {
                return;
            }

            if (index < 0 || index >= AssemblyList_.Count)
            {
                AssemblyList_.Add(assembly);
            }
            else
            {
                AssemblyList_.Insert(index, assembly);
            }
        }

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
        
        public static Type GetTypeWithAssembly(Assembly[] assemblyList, string typeName)
        {
            foreach (var assembly in assemblyList)
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        public static Type GetTypeWithAssembly(string typeName)
        {
            foreach (var assembly in AssemblyList_)
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
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
    }
}