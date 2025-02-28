using System;
using System.Diagnostics;

namespace LiteQuark.Runtime
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class LiteEnumFlagsAttribute : Attribute
    {
        public LiteEnumFlagsAttribute()
        {
        }
    }
}