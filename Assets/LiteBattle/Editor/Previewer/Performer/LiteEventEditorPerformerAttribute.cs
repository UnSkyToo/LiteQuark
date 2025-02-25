using System;
using System.Diagnostics;

namespace LiteBattle.Editor
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class LiteEventEditorPerformerAttribute : Attribute
    {
        public Type BindType { get; }
        
        public LiteEventEditorPerformerAttribute(Type bindType)
        {
            BindType = bindType;
        }
    }
}