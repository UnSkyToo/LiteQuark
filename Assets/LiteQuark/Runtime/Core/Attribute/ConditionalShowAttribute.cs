using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public sealed class ConditionalShowAttribute : PropertyAttribute
    {
        public readonly string[] SourceFields = new string[] { };
        public readonly object[] Values = new object[] { };
        public readonly bool[] Inverses = new bool[] { };
        
        public ConditionalShowAttribute(string sourceField, object value)
            : this(sourceField, value, false)
        {
        }
        
        public ConditionalShowAttribute(string sourceField1, object value1, string sourceField2, object value2)
            : this(new[] { sourceField1, sourceField2 }, new[] { value1, value2 }, new[] { false, false })
        {
        }

        public ConditionalShowAttribute(string sourceField, object value, bool inverse)
            : this(new[] { sourceField }, new[] { value }, new[] { inverse })
        {
        }

        public ConditionalShowAttribute(string[] conditionalSourceFields, object[] values, bool[] inverses)
        {
            this.SourceFields = conditionalSourceFields;
            this.Values = values;
            this.Inverses = inverses;
        }
    }
}