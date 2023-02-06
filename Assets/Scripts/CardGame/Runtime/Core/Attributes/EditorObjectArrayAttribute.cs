using System;

namespace LiteCard
{
    public enum EditorObjectArrayType
    {
        CardCastCheck,
        CardCastTarget,
        CardUpgrade,
        MatchFilter,
        Modifier,
        BuffTrigger,
    }
    
    public sealed class EditorObjectArrayResult
    {
        public string[] Display { get; }
        public Type[] Value { get; }
        public object[][] Attrs { get; }

        public static EditorObjectArrayResult None { get; } = new EditorObjectArrayResult();

        public EditorObjectArrayResult()
            : this(Array.Empty<string>(), Array.Empty<Type>())
        {
        }

        public EditorObjectArrayResult(string display1, Type value1)
            : this(new[] { display1 }, new[] { value1 })
        {
        }

        public EditorObjectArrayResult(string display1, string display2, Type value1, Type value2)
            : this(new[] { display1, display2 }, new[] { value1, value2 })
        {
        }

        public EditorObjectArrayResult(string display1, string display2, string display3, Type value1, Type value2, Type value3)
            : this(new[] { display1, display2, display3 }, new[] { value1, value2, value3 })
        {
        }

        public EditorObjectArrayResult(string display1, string display2, string display3, string display4, Type value1, Type value2, Type value3, Type value4)
            : this(new[] { display1, display2, display3, display4 }, new[] { value1, value2, value3, value4 })
        {
        }

        public EditorObjectArrayResult(string[] display, Type[] value)
        {
            Display = display;
            Value = value;
            Attrs = new object[value.Length][];
        }

        public void SetAttrs(int index, object attr)
        {
            SetAttrs(index, new [] { attr });
        }

        public void SetAttrs(int index, object[] attrs)
        {
            Attrs[index] = attrs;
        }
    }
    
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class EditorObjectArrayAttribute : Attribute
    {
        public EditorObjectArrayType Type { get; }
        public string Binder { get; }
        
        public EditorObjectArrayAttribute(EditorObjectArrayType type, string binder)
        {
            Type = type;
            Binder = binder;
        }
    }
}