using System;

namespace LiteCard
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class EditorDataReadOnlyAttribute : Attribute
    {
        public EditorDataReadOnlyAttribute()
        {
        }
    }
}