using System;
using System.Diagnostics;

namespace LiteBattle.Runtime
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
        CustomPopupList,
        LiteStateNameList,
        AnimationNameList,
        InputKeyList,
        OptionalType,
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