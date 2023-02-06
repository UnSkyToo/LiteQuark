using System;

namespace LiteCard
{
    public enum EditorDataPopupType
    {
        Card = 0,
        Buff = 1,
        Modifier = 2,
        Match = 3
    }

    public sealed class EditorDataPopupResult
    {
        public string[] Display { get; }
        public int[] Value { get; }

        public EditorDataPopupResult(string[] display, int[] value)
        {
            Display = display;
            Value = value;
        }
    }
    
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class EditorDataPopupAttribute : Attribute
    {
        public EditorDataPopupType Type { get; }

        public EditorDataPopupAttribute(EditorDataPopupType type)
        {
            Type = type;
        }
    }
}