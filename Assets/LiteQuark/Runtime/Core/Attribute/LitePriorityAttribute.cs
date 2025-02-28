using System;
using System.Diagnostics;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// 编辑器绘制属性优先级，越大越先绘制
    /// <para>可用于排序object中的propery、filed或者OptionalType中的type（针对class）</para>
    /// </summary>
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