using System;
using System.Reflection;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class ReflectionUtils
    {
        public static MethodInfo GetMethod(Type type, string methodName, BindingFlags flags)
        {
            var methodInfo = type.GetMethod(methodName, flags);
            if (methodInfo == null)
            {
                Debug.LogWarning($"can't find {methodName} method in {type}");
                return null;
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

            return null;
        }

        public static object GetFieldValue(Type type, string fieldName, BindingFlags flags)
        {
            return GetFieldValue(type, null, fieldName, flags);
        }
        
        public static object GetFieldValue(object instance, string fieldName, BindingFlags flags)
        {
            return GetFieldValue(instance.GetType(), instance, fieldName, flags);
        }
        
        public static object GetFieldValue(Type type, object instance, string fieldName, BindingFlags flags)
        {
            var fieldInfo = type.GetField(fieldName, flags);
            if (fieldInfo == null)
            {
                Debug.LogWarning($"can't find {fieldName} field in {type}");
                return null;
            }
            
            var fieldVal = fieldInfo.GetValue(instance);
            if (fieldVal == null)
            {
                Debug.LogWarning($"can't get {fieldName} instance value");
                return null;
            }

            return fieldVal;
        }
        
        public static object GetPropertyValue(Type type, string propertyName, BindingFlags flags)
        {
            return GetPropertyValue(type, null, propertyName, flags);
        }
        
        public static object GetPropertyValue(object instance, string propertyName, BindingFlags flags)
        {
            return GetPropertyValue(instance.GetType(), instance, propertyName, flags);
        }
        
        public static object GetPropertyValue(Type type, object instance, string propertyName, BindingFlags flags)
        {
            var propertyInfo = type.GetProperty(propertyName, flags);
            if (propertyInfo == null)
            {
                Debug.LogWarning($"can't find {propertyName} property in {type}");
                return null;
            }

            var propertyVal = propertyInfo.GetValue(instance);
            if (propertyVal == null)
            {
                Debug.LogWarning($"can't get {propertyName} instance value");
                return null;
            }

            return propertyVal;
        }

        public static void InvokeMethod(Type type, string methodName, BindingFlags flags)
        {
            InvokeMethod(type, null, methodName, flags);
        }

        public static void InvokeMethod(object instance, string methodName, BindingFlags flags)
        {
            InvokeMethod(instance.GetType(), instance, methodName, flags);
        }
        
        public static void InvokeMethod(Type type, object instance, string methodName, BindingFlags flags)
        {
            var methodInfo = type.GetMethod(methodName, flags);
            if (methodInfo == null)
            {
                Debug.LogWarning($"can't find {methodName} method in {type}");
                return;
            }
            
            methodInfo.Invoke(instance, null);
        }
    }
}