using System;
using System.Diagnostics;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// 配合LitePropertyType.OptionalType使用
    /// <para>配置可选类LiteOptionalType(typeof(IData), "数据")来声明类型</para>
    /// 注意：需要继承<b>IHasData</b>来决定编辑器是否数据内容
    /// <param name="BaseType">基类类型（推荐用接口）</param>
    /// <param name="DataTitle">数据字段的标题</param>
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class LiteOptionalTypeAttribute : Attribute
    {
        public Type BaseType { get; }
        public string DataTitle { get; }
        
        /// <param name="baseType">基类类型（推荐用接口）</param>
        /// <param name="dataTitle">数据字段的标题</param>
        public LiteOptionalTypeAttribute(Type baseType, string dataTitle)
        {
            BaseType = baseType;
            DataTitle = dataTitle;
        }
    }
    
    /// <summary>
    /// 配合LitePropertyType.OptionalType使用
    /// <para>配置可选类LiteOptionalType(typeof(IData), "数据")来声明类型</para>
    /// 注意：需要继承<b>IHasData</b>来决定编辑器是否数据内容
    /// <param name="BaseType">基类类型（推荐用接口）</param>
    /// <param name="DataTitle">数据字段的标题</param>
    /// <param name="DefaultType">添加数据时，默认实例的子类类型</param>
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class LiteOptionalTypeListAttribute : Attribute
    {
        public Type BaseType { get; }
        public string DataTitle { get; }
        public Type DefaultType { get; }
        
        /// <param name="baseType">基类类型（推荐用接口）</param>
        /// <param name="dataTitle">数据字段的标题</param>
        /// <param name="defaultType">添加数据时，默认实例的子类类型</param>
        public LiteOptionalTypeListAttribute(Type baseType, string dataTitle, Type defaultType)
        {
            BaseType = baseType;
            DataTitle = dataTitle;
            DefaultType = defaultType;
        }
    }
}