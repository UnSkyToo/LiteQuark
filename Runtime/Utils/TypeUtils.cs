using System;
using System.Collections.Generic;
using System.Reflection;

namespace LiteQuark.Runtime
{
    public static class TypeUtils
    {
        private static readonly List<Assembly> AssemblyList = new List<Assembly>();

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
            
            if (AssemblyList.Contains(assembly))
            {
                return;
            }

            if (index < 0 || index >= AssemblyList.Count)
            {
                AssemblyList.Add(assembly);
            }
            else
            {
                AssemblyList.Insert(index, assembly);
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
            foreach (var assembly in AssemblyList)
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

        public static bool IsAssignableTo<T>(this Type type)
        {
            return typeof(T).IsAssignableFrom(type);
        }
        
        public static bool IsListType(Type type)
        {
            return type.IsGenericType && type.GenericTypeArguments.Length == 1;
        }
        
        public static bool IsDictionaryType(Type type)
        {
            return type.IsGenericType && type.GenericTypeArguments.Length == 2;
        }
        
        public static Type GetElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.GenericTypeArguments.Length > 1)
            {
                return type.GenericTypeArguments[1];
            }

            if (type.GenericTypeArguments.Length > 0)
            {
                return type.GenericTypeArguments[0];
            }

            return null;
        }

        public static bool CanCreateType(Type type)
        {
            return !(type.IsAbstract || type.IsInterface);
        }
        
        public static object CreateInstance(Type type)
        {
            if (type.IsInterface)
            {
                LLog.Error("Can't create instance of interface type {0}", type.Name);
                return null;
            }

            if (type.IsAbstract)
            {
                LLog.Error("Can't create instance of abstract type {0}", type.Name);
                return null;
            }
            
            if (type.IsArray)
            {
                return Array.CreateInstance(GetElementType(type), 0);
            }
            
            if (IsListType(type))
            {
                return CreateGenericList(GetElementType(type));
            }

            if (IsDictionaryType(type))
            {
                return CreateGenericDictionary(type.GenericTypeArguments[0], type.GenericTypeArguments[1]);
            }
            
            return type == typeof(string) ? string.Empty : Activator.CreateInstance(type);
        }

        public static object CreateInstance(Type type, int count)
        {
            if (type.IsInterface)
            {
                LLog.Error("Can't create instance of interface type {0}", type.Name);
                return null;
            }

            if (type.IsAbstract)
            {
                LLog.Error("Can't create instance of abstract type {0}", type.Name);
                return null;
            }
            
            if (type.IsArray)
            {
                return Array.CreateInstance(GetElementType(type), count);
            }

            if (IsListType(type))
            {
                return CreateGenericList(GetElementType(type));
            }

            if (IsDictionaryType(type))
            {
                return CreateGenericDictionary(type.GenericTypeArguments[0], type.GenericTypeArguments[1]);
            }
            
            LLog.Error("Can't create instance of array type {0}", type.Name);
            return null;
        }

        public static T CreateInstance<T>()
        {
            return (T) CreateInstance(typeof(T));
        }
        
        public static object CreateGenericList(Type elementType)
        {
            var typeList = typeof(List<>);
            var genericType = typeList.MakeGenericType(elementType);
            return Activator.CreateInstance(genericType);
        }

        public static object CreateGenericDictionary(Type keyType, Type valueType)
        {
            var typeDict = typeof(Dictionary<,>);
            var genericType = typeDict.MakeGenericType(keyType, valueType);
            return Activator.CreateInstance(genericType);
        }

        public static T CreateEntryInstance<T>(LiteTypeEntryData<T> entryData)
        {
            if (entryData == null || entryData.Disabled)
            {
                return default;
            }

            var logicType = System.Type.GetType(entryData.AssemblyQualifiedName);
            if (logicType == null)
            {
                throw new System.Exception($"Can't not find class type : {entryData.AssemblyQualifiedName}");
            }

            if (System.Activator.CreateInstance(logicType) is not T instanceType)
            {
                throw new System.Exception($"Incorrect {typeof(T)} type : {logicType.AssemblyQualifiedName}");
            }

            return instanceType;
        }
        
        public static T GetAttribute<T>(Type type, object[] attrs) where T : Attribute
        {
            T result = null;
            if (attrs != null)
            {
                result = (T)Array.Find(attrs, static t => t is T);
            }

            if (result == null)
            {
                result = type?.GetCustomAttribute<T>();
            }
            return result;
        }
        
        public static string GetTypeDisplayName(Type type)
        {
            if (type == null)
            {
                return "null";
            }
            
            var labelAttr = GetAttribute<LiteLabelAttribute>(type, null);
            return labelAttr != null ? labelAttr.Label : type.Name;
        }
        
        public static List<string> TypeListToString(List<Type> typeList)
        {
            var results = new List<string>();

            foreach (var type in typeList)
            {
                results.Add(GetTypeDisplayName(type));
            }

            return results;
        }
        
        public static Type[] PrioritySort(Type[] sortList)
        {
            var list = new List<Type>(sortList);
            PrioritySort(list);
            return list.ToArray();
        }
        
        public static void PrioritySort(List<Type> sortList)
        {
            sortList.Sort(static (a, b) =>
            {
                var priorityA = GetPriorityFromType(a);
                var priorityB = GetPriorityFromType(b);

                if (priorityA == priorityB)
                {
                    return 0;
                }

                if (priorityA > priorityB)
                {
                    return -1;
                }

                return 1;
            });
        }

        public static T[] PrioritySort<T>(T[] sortList) where T : MemberInfo
        {
            var list = new List<T>(sortList);
            PrioritySort(list);
            return list.ToArray();
        }
        
        public static void PrioritySort<T>(List<T> sortList) where T : MemberInfo
        {
            sortList.Sort(static (a, b) =>
            {
                var priorityA = GetPriorityFromType(a);
                var priorityB = GetPriorityFromType(b);

                if (priorityA == priorityB)
                {
                    return 0;
                }

                if (priorityA > priorityB)
                {
                    return -1;
                }

                return 1;
            });
        }
        
        public static uint GetPriorityFromType(Type type)
        {
            var priorityAttr = type.GetCustomAttribute<LitePriorityAttribute>();
            var value = priorityAttr != null ? priorityAttr.Value : 0;
            return value;
        }
        
        public static uint GetPriorityFromType<T>(T info) where T : MemberInfo
        {
            var priorityAttr = info.GetCustomAttribute<LitePriorityAttribute>();
            var value = priorityAttr != null ? priorityAttr.Value : 0;
            return value;
        }
    }
}