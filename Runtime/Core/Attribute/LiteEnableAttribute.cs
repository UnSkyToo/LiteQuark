using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// 标记LiteEnableCheckerAttribute的数据源，如果没有标记则默认为满足条件
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class LiteEnableSourceAttribute : Attribute
    {
        public LiteEnableSourceAttribute()
        {
        }

        public static Dictionary<string, object> GetSourceData(object target)
        {
            var type = target.GetType();
            var fields = type.GetFields();
            var properties = type.GetProperties();
            var data = new Dictionary<string, object>();

            foreach (var field in fields)
            {
                var sourceAttr = field.GetCustomAttribute<LiteEnableSourceAttribute>();
                if (sourceAttr != null)
                {
                    data[field.Name] = field.GetValue(target);
                }
            }
            
            foreach (var property in properties)
            {
                var sourceAttr = property.GetCustomAttribute<LiteEnableSourceAttribute>();
                if (sourceAttr != null)
                {
                    data[property.Name] = property.GetValue(target);
                }
            }

            return data;
        }
    }
    
    /// <summary>
    /// 控制字段是否在编辑器上展示。
    /// <para>其中CheckName和CheckValue决定了依据哪个字段进行控制，该字段需要被LiteEnableSourceAttribute标记。</para>
    /// CompareEqual字段决定CheckValue是比较相等还是不相等，默认为相等。
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public sealed class LiteEnableCheckerAttribute : Attribute
    {
        public string CheckName { get; }
        public object CheckValue { get; }
        public bool CompareEqual { get; }
        
        public LiteEnableCheckerAttribute(string checkName, object checkValue, bool compareEqual = true)
        {
            CheckName = checkName;
            CheckValue = checkValue;
            CompareEqual = compareEqual;
        }

        public bool Check(Dictionary<string, object> data)
        {
            if (data.ContainsKey(CheckName))
            {
                var isEqual = TypeUtils.Equal(CheckValue, data[CheckName]);
                return CompareEqual ? isEqual : !isEqual;
            } 

            return true;
        }

        private static bool Check(IEnumerable<LiteEnableCheckerAttribute> checkerAttr, Dictionary<string, object> sourceData)
        {
            foreach (var attr in checkerAttr)
            {
                if (!attr.Check(sourceData))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Check(MemberInfo info, Dictionary<string, object> sourceData)
        {
            var checkerAttr = info.GetCustomAttributes<LiteEnableCheckerAttribute>();
            return Check(checkerAttr, sourceData);
        }
    }
}