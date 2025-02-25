using System;
using System.Reflection;

namespace LiteBattle.Runtime
{
    public static class LiteReflectionHelper
    {
        public static PropertyInfo GetPropertyInfo(object instance, string name)
        {
            if (instance == null)
            {
                return null;
            }

            var type = instance.GetType();
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
        
        public static PropertyInfo GetPropertyInfo(Type type, string name)
        {
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        }
        
        public static object GetPropertyValue(object instance, string name)
        {
            if (instance == null)
            {
                return null;
            }
            
            var propertyInfo = GetPropertyInfo(instance, name);
            if (propertyInfo == null)
            {
                throw new Exception($"can't find {instance.GetType()}.{name}");
            }
            
            var value = propertyInfo.GetValue(instance);
            return value;
        }

        public static void SetPropertyValue(object instance, string name, object value)
        {
            if (instance == null)
            {
                return;
            }
            
            var propertyInfo = GetPropertyInfo(instance, name);
            if (propertyInfo == null)
            {
                throw new Exception($"can't find {instance.GetType()}.{name}");
            }
            
            propertyInfo.SetValue(instance, value);
        }

        public static object GetPropertyValue(Type type, string name)
        {
            var propertyInfo = GetPropertyInfo(type, name);
            if (propertyInfo == null)
            {
                throw new Exception($"can't find {type}.{name}");
            }
            
            var value = propertyInfo.GetValue(null);
            return value;
        }
        
        public static void SetPropertyValue(Type type, string name, object value)
        {
            var propertyInfo = GetPropertyInfo(type, name);
            if (propertyInfo == null)
            {
                throw new Exception($"can't find {type}.{name}");
            }
            
            propertyInfo.SetValue(null, value);
        }
        
        public static FieldInfo GetFieldInfo(object instance, string name)
        {
            if (instance == null)
            {
                return null;
            }

            var type = instance.GetType();
            return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static FieldInfo GetFieldInfo(Type type, string name)
        {
            return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        }
        
        public static object GetFieldValue(object instance, string name)
        {
            if (instance == null)
            {
                return null;
            }
            
            var fieldInfo = GetFieldInfo(instance, name);
            if (fieldInfo == null)
            {
                throw new Exception($"can't find {instance.GetType()}.{name}");
            }
            
            var value = fieldInfo.GetValue(instance);
            return value;
        }
        
        public static void SetFieldValue(object instance, string name, object value)
        {
            if (instance == null)
            {
                return;
            }

            var fieldInfo = GetFieldInfo(instance, name);
            if (fieldInfo == null)
            {
                throw new Exception($"can't find {instance.GetType()}.{name}");
            }
            
            fieldInfo.SetValue(instance, value);
        }

        public static object GetFieldValue(Type type, string name)
        {
            var fieldInfo = GetFieldInfo(type, name);
            if (fieldInfo == null)
            {
                throw new Exception($"can't find {type}.{name}");
            }

            var value = fieldInfo.GetValue(null);
            return value;
        }

        public static void SetFiledValue(Type type, string name, object value)
        {
            var fieldInfo = GetFieldInfo(type, name);
            if (fieldInfo == null)
            {
                throw new Exception($"can't find {type}.{name}");
            }
            
            fieldInfo.SetValue(null, value);
        }

        public static object InvokeMethod(Type type, string name, object[] paramList)
        {
            var methodInfo = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (methodInfo == null)
            {
                throw new Exception($"can't find {name} in {type}");
            }

            return methodInfo.Invoke(null, paramList);
        }

        public static object InvokeMethod(object instance, string name, object[] paramList)
        {
            if (instance == null)
            {
                return null;
            }

            var type = instance.GetType();
            
            var methodInfo = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo == null)
            {
                throw new Exception($"can't find {name} in {type}");
            }

            return methodInfo.Invoke(instance, paramList);
        }
        
        public static object InvokeGenericMethod<T>(Type type, string name, object[] paramList)
        {
            var methodInfo = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (methodInfo == null)
            {
                throw new Exception($"can't find {name} in {type}");
            }
            
            var method = methodInfo.MakeGenericMethod(typeof(T));
            return method.Invoke(null, paramList);
        }

        public static object InvokeGenericMethod<T>(object instance, string name, object[] paramList)
        {
            if (instance == null)
            {
                return null;
            }

            var type = instance.GetType();
            
            var methodInfo = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo == null)
            {
                throw new Exception($"can't find {name} in {type}");
            }

            var method = methodInfo.MakeGenericMethod(typeof(T));
            return method.Invoke(instance, paramList);
        }
    }
}