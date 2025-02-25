using System;
using System.Diagnostics;

namespace LiteBattle.Runtime
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class LiteDelayedAttribute : Attribute
    {
        public LiteDelayedAttribute()
        {
        }
    }
}