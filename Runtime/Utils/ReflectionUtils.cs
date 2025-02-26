using System;
using System.Reflection;

namespace LiteQuark.Runtime
{
    public static class ReflectionUtils
    {
        public const BindingFlags DefaultBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
        
        public static MethodInfo GetMethod(Type type, string methodName, BindingFlags flags = DefaultBindingFlags)
        {
            var methodInfo = type.GetMethod(methodName, flags);
            if (methodInfo == null)
            {
                LLog.Warning($"can't find {methodName} method in {type}");
                return null;
            }

            return methodInfo;
        }

        public static MethodInfo GetMethod(Type type, string methodName, int paramCount, BindingFlags flags = DefaultBindingFlags)
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

            return null;
        }

        public static object GetFieldValue(Type type, string fieldName, BindingFlags flags = DefaultBindingFlags)
        {
            return GetFieldValue(type, null, fieldName, flags);
        }
        
        public static object GetFieldValue(object instance, string fieldName, BindingFlags flags = DefaultBindingFlags)
        {
            return GetFieldValue(instance.GetType(), instance, fieldName, flags);
        }
        
        public static object GetFieldValue(Type type, object instance, string fieldName, BindingFlags flags = DefaultBindingFlags)
        {
            var fieldInfo = type.GetField(fieldName, flags);
            if (fieldInfo == null)
            {
                LLog.Warning($"can't find {fieldName} field in {type}");
                return null;
            }
            
            var fieldVal = fieldInfo.GetValue(instance);
            if (fieldVal == null)
            {
                LLog.Warning($"can't get {fieldName} instance value");
                return null;
            }

            return fieldVal;
        }
        
        public static object GetPropertyValue(Type type, string propertyName, BindingFlags flags = DefaultBindingFlags)
        {
            return GetPropertyValue(type, null, propertyName, flags);
        }
        
        public static object GetPropertyValue(object instance, string propertyName, BindingFlags flags = DefaultBindingFlags)
        {
            return GetPropertyValue(instance.GetType(), instance, propertyName, flags);
        }
        
        public static object GetPropertyValue(Type type, object instance, string propertyName, BindingFlags flags = DefaultBindingFlags)
        {
            var propertyInfo = type.GetProperty(propertyName, flags);
            if (propertyInfo == null)
            {
                LLog.Warning($"can't find {propertyName} property in {type}");
                return null;
            }

            var propertyVal = propertyInfo.GetValue(instance);
            if (propertyVal == null)
            {
                LLog.Warning($"can't get {propertyName} instance value");
                return null;
            }

            return propertyVal;
        }

        public static void InvokeMethod(Type type, string methodName, BindingFlags flags = DefaultBindingFlags)
        {
            InvokeMethod(type, null, methodName, flags);
        }

        public static void InvokeMethod(object instance, string methodName, BindingFlags flags = DefaultBindingFlags)
        {
            InvokeMethod(instance.GetType(), instance, methodName, flags);
        }
        
        public static void InvokeMethod(Type type, object instance, string methodName, BindingFlags flags = DefaultBindingFlags)
        {
            var methodInfo = type.GetMethod(methodName, flags);
            if (methodInfo == null)
            {
                LLog.Warning($"can't find {methodName} method in {type}");
                return;
            }
            
            methodInfo.Invoke(instance, null);
        }
    }
}