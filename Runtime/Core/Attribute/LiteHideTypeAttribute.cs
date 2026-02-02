using System;

namespace LiteQuark.Runtime
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public sealed class LiteHideTypeAttribute : Attribute
    {
        public LiteHideTypeAttribute()
        {
        }
    }
}