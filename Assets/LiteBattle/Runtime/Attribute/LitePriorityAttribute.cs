using System;
using System.Diagnostics;

namespace LiteBattle.Runtime
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class LitePriorityAttribute : Attribute
    {
        public uint Value { get; }
    
        public LitePriorityAttribute(uint value)
        {
            Value = value;
        }
    }
}