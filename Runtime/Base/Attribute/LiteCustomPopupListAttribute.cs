using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LiteQuark.Runtime
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class LiteCustomPopupListAttribute : Attribute
    {
        public Func<List<string>> GetListFunc { get; }
        
        public LiteCustomPopupListAttribute(Func<List<string>> getListFunc)
        {
            GetListFunc = getListFunc;
        }
    }
}