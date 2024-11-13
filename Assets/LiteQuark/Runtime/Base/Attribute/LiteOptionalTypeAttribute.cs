using System;
using System.Diagnostics;

namespace LiteQuark.Runtime
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class LiteOptionalTypeAttribute : Attribute
    {
        public Type BaseType { get; }
        public string DataTitle { get; }
        
        public LiteOptionalTypeAttribute(Type baseType, string dataTitle)
        {
            BaseType = baseType;
            DataTitle = dataTitle;
        }
    }
    
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class LiteOptionalTypeListAttribute : Attribute
    {
        public Type BaseType { get; }
        public Type DefaultType { get; }
        public string ElementTitle { get; }
        public string DataTitle { get; }
        
        public LiteOptionalTypeListAttribute(Type baseType, Type defaultType, string elementTitle, string dataTitle)
        {
            BaseType = baseType;
            DefaultType = defaultType;
            ElementTitle = elementTitle;
            DataTitle = dataTitle;
        }
    }
}