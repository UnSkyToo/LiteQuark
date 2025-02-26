using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace LiteQuark.Runtime
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class LiteCustomPopupListAttribute : Attribute
    {
        public string MethodName { get; }
        public Type DeclaringType { get; }
        
        public LiteCustomPopupListAttribute(Type declaringType, string methodName)
        {
            DeclaringType = declaringType;
            MethodName = methodName;
        }

        public List<string> GetList()
        {
            var method = ReflectionUtils.GetMethod(DeclaringType, MethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null || !typeof(List<string>).IsAssignableFrom(method.ReturnType))
            {
                throw new InvalidOperationException($"Method {MethodName} not found or does not return List<string>.");
            }

            return method.Invoke(null, null) as List<string>;
        }
    }
}