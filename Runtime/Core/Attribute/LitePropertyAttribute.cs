using System;
using System.Diagnostics;

namespace LiteQuark.Runtime
{
    public enum LitePropertyType
    {
        // Primitive
        Bool,
        Int,
        Long,
        Float,
        Double,
        String,
        Enum,
        // Unity
        Rect,
        RectInt,
        Vector2,
        Vector2Int,
        Vector3,
        Vector3Int,
        Vector4,
        Color,
        GameObject,
        // Other,
        List,
        Object,
        ObjectList,
        CustomPopupList,
        /// <summary>
        /// 可选类（可以选择所有继承自制定BaseType的类）
        /// <para>例如 public IData Data</para>
        /// 必须配合属性LiteOptionalType(typeof(IData), "数据")来声明类型
        /// </summary>
        OptionalType,
        /// <summary>
        /// 可选类列表，同OptionalType
        /// </summary>
        OptionalTypeList,
    }
    
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class LitePropertyAttribute : Attribute
    {
        public string Name { get; }
        public LitePropertyType Type { get; }
        
        public LitePropertyAttribute(string name, LitePropertyType type)
        {
            Name = name;
            Type = type;
        }
    }
}