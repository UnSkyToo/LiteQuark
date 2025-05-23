﻿using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public sealed class ConditionalHideAttribute : PropertyAttribute
    {
        public readonly string ConditionalSourceField = "";
        public readonly string ConditionalSourceField2 = "";
        public readonly string[] ConditionalSourceFields = new string[] { };
        public readonly bool[] ConditionalSourceFieldInverseBools = new bool[] { };
        public readonly bool HideInInspector = false;
        public readonly bool Inverse = false;
        public readonly bool UseOrLogic = false;

        public readonly bool InverseCondition1 = false;
        public readonly bool InverseCondition2 = false;
        
        public ConditionalHideAttribute(string conditionalSourceField)
        {
            this.ConditionalSourceField = conditionalSourceField;
            this.HideInInspector = false;
            this.Inverse = false;
        }

        public ConditionalHideAttribute(string conditionalSourceField, bool hideInInspector)
        {
            this.ConditionalSourceField = conditionalSourceField;
            this.HideInInspector = hideInInspector;
            this.Inverse = false;
        }

        public ConditionalHideAttribute(string conditionalSourceField, bool hideInInspector, bool inverse)
        {
            this.ConditionalSourceField = conditionalSourceField;
            this.HideInInspector = hideInInspector;
            this.Inverse = inverse;
        }

        public ConditionalHideAttribute(bool hideInInspector = false)
        {
            this.ConditionalSourceField = "";
            this.HideInInspector = hideInInspector;
            this.Inverse = false;
        }

        public ConditionalHideAttribute(string[] conditionalSourceFields, bool[] conditionalSourceFieldInverseBools, bool hideInInspector, bool inverse)
        {
            this.ConditionalSourceFields = conditionalSourceFields;
            this.ConditionalSourceFieldInverseBools = conditionalSourceFieldInverseBools;
            this.HideInInspector = hideInInspector;
            this.Inverse = inverse;
        }

        public ConditionalHideAttribute(string[] conditionalSourceFields, bool hideInInspector, bool inverse)
        {
            this.ConditionalSourceFields = conditionalSourceFields;
            this.HideInInspector = hideInInspector;
            this.Inverse = inverse;
        }
    }
}