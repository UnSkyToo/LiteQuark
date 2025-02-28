using System;
using System.Diagnostics;

namespace LiteQuark.Runtime
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class LiteIntRangeAttribute : Attribute
    {
        public int Min { get; }
        public int Max { get; }
        
        public LiteIntRangeAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }
    
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class LiteFloatRangeAttribute : Attribute
    {
        public float Min { get; }
        public float Max { get; }
        
        public LiteFloatRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}