using System;
using System.Reflection;

namespace LiteQuark.Runtime
{
    public static class ReflectionUtils
    {
        public const BindingFlags DefaultInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        public const BindingFlags DefaultStaticFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        
        public static MethodInfo GetMethod(Type type, string methodName, BindingFlags flags)
        {
            var methodInfo = type.GetMethod(methodName, flags);
            if (methodInfo == null)
            {
                throw new Exception($"can't find {methodName} method in {type}");
            }

            return methodInfo;
        }

        public static MethodInfo GetMethod(Type type, string methodName, int paramCount, BindingFlags flags)
        {
            var methods = type.GetMethods(flags);

            foreach (var method in methods)
            {
                if (method.Name == methodName)
                {
                    if (method.GetParameters().Length == paramCount)
                    {
                        return method;
                    }
                }
            }

            throw new Exception($"can't find {methodName} with {paramCount} params method in {type}");
        }
        
        public static void InvokeMethod(Type type, string methodName, BindingFlags flags = DefaultStaticFlags)
        {
            InvokeMethod(type, null, methodName, flags);
        }

        public static void InvokeMethod(object instance, string methodName, BindingFlags flags = DefaultInstanceFlags)
        {
            InvokeMethod(instance.GetType(), instance, methodName, flags);
        }
        
        public static void InvokeMethod(Type type, object instance, string methodName, BindingFlags flags)
        {
            var methodInfo = GetMethod(type, methodName, flags);
            methodInfo.Invoke(instance, null);
        }
        
        public static object InvokeGenericMethod<T>(Type type, string methodName, object[] paramList, BindingFlags flags)
        {
            var methodInfo = GetMethod(type, methodName, flags);
            var method = methodInfo.MakeGenericMethod(typeof(T));
            return method.Invoke(null, paramList);
        }

        public static object InvokeGenericMethod<T>(object instance, string methodName, object[] paramList, BindingFlags flags)
        {
            if (instance == null)
            {
                return null;
            }

            var type = instance.GetType();
            var methodInfo = GetMethod(type, methodName, flags);
            var method = methodInfo.MakeGenericMethod(typeof(T));
            return method.Invoke(instance, paramList);
        }
        
        public static FieldInfo GetFieldInfo(object instance, string fieldName, BindingFlags flags = DefaultInstanceFlags)
        {
            if (instance == null)
            {
                return null;
            }

            var type = instance.GetType();
            return GetFieldInfo(type, fieldName, flags);
        }
        
        public static FieldInfo GetFieldInfo(Type type, string fieldName, BindingFlags flags = DefaultStaticFlags)
        {
            return type.GetField(fieldName, flags);
        }

        public static object GetFieldValue(Type type, string fieldName, BindingFlags flags = BindingFlags.Static)
        {
            return GetFieldValue(type, null, fieldName, flags);
        }
        
        public static object GetFieldValue(object instance, string fieldName, BindingFlags flags = BindingFlags.Instance)
        {
            return GetFieldValue(instance.GetType(), instance, fieldName, flags);
        }
        
        public static object GetFieldValue(Type type, object instance, string fieldName, BindingFlags flags)
        {
            var fieldInfo = GetFieldInfo(type, fieldName, flags);
            if (fieldInfo == null)
            {
                throw new Exception($"can't find {type}.{fieldName}");
            }
            
            var fieldVal = fieldInfo.GetValue(instance);
            return fieldVal;
        }
        
        public static void SetFieldValue(Type type, string fieldName, object fieldValue, BindingFlags flags = DefaultStaticFlags)
        {
            SetFieldValue(type, null, fieldName, fieldValue, flags);
        }
        
        public static void SetFieldValue(object instance, string fieldName, object fieldValue, BindingFlags flags = DefaultInstanceFlags)
        {
            if (instance == null)
            {
                return;
            }
            
            SetFieldValue(instance.GetType(), instance, fieldName, fieldValue, flags);
        }
        
        public static void SetFieldValue(Type type, object instance, string fieldName, object fieldValue, BindingFlags flags)
        {
            var fieldInfo = GetFieldInfo(type, fieldName, flags);
            if (fieldInfo == null)
            {
                throw new Exception($"can't find {type}.{fieldName}");
            }
            
            fieldInfo.SetValue(instance, fieldValue);
        }
        
        public static PropertyInfo GetPropertyInfo(object instance, string propertyName, BindingFlags flags = DefaultStaticFlags)
        {
            if (instance == null)
            {
                return null;
            }

            var type = instance.GetType();
            return GetPropertyInfo(type, propertyName, flags);
        }
        
        public static PropertyInfo GetPropertyInfo(Type type, string propertyName, BindingFlags flags = DefaultInstanceFlags)
        {
            return type.GetProperty(propertyName, flags);
        }
        
        public static object GetPropertyValue(Type type, string propertyName, BindingFlags flags = DefaultStaticFlags)
        {
            return GetPropertyValue(type, null, propertyName, flags | BindingFlags.Static);
        }
        
        public static object GetPropertyValue(object instance, string propertyName, BindingFlags flags = DefaultInstanceFlags)
        {
            return GetPropertyValue(instance.GetType(), instance, propertyName, flags);
        }
        
        public static object GetPropertyValue(Type type, object instance, string propertyName, BindingFlags flags)
        {
            var propertyInfo = GetPropertyInfo(type, propertyName, flags);
            if (propertyInfo == null)
            {
                throw new Exception($"can't find {type}.{propertyName}");
            }

            var propertyVal = propertyInfo.GetValue(instance);
            return propertyVal;
        }
        
        public static void SetPropertyValue(Type type, string propertyName, object propertyValue, BindingFlags flags = DefaultStaticFlags)
        {
            SetPropertyValue(type, null, propertyName, propertyValue, flags | BindingFlags.Static);
        }
        
        public static void SetPropertyValue(object instance, string propertyName, object propertyValue, BindingFlags flags = DefaultInstanceFlags)
        {
            if (instance == null)
            {
                return;
            }
            
            SetPropertyValue(instance.GetType(), instance, propertyName, propertyValue, flags);
        }
        
        public static void SetPropertyValue(Type type, object instance, string propertyName, object propertyValue, BindingFlags flags)
        {
            var propertyInfo = GetPropertyInfo(type, propertyName, flags);
            if (propertyInfo == null)
            {
                throw new Exception($"can't find {type}.{propertyName}");
            }
            
            propertyInfo.SetValue(instance, propertyValue);
        }
    }
}