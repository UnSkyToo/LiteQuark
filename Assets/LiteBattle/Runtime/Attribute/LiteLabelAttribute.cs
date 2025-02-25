using System;
using System.Diagnostics;

namespace LiteBattle.Runtime
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class LiteLabelAttribute : Attribute
    {
        public string Label { get; }
    
        public LiteLabelAttribute(string label)
        {
            Label = label;
        }
    }
}