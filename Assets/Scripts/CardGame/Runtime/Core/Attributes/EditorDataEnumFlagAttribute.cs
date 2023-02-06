using System;

namespace LiteCard
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class EditorDataEnumFlagAttribute : Attribute
    {
        public EditorDataEnumFlagAttribute()
        {
        }
    }
}