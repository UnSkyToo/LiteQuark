using System;
using System.Reflection;
using LiteQuark.Runtime;
using UnityEditor;

namespace LiteQuark.Editor
{
    internal static class DevelopmentSettingsFields
    {
        internal enum Access { Inherited, Override, Foldout }
        internal delegate bool Visitor(SerializedProperty property, FieldInfo field, Access access, int depth);

        // The visitor controls expansion. Only a framework BuildValue is offered an editable drawer.
        internal static void Visit(SerializedProperty parent, Type owner, Visitor visitor, int depth = 0, bool conditionalElements = false)
        {
            if (parent.propertyType == SerializedPropertyType.ManagedReference)
            {
                owner = parent.managedReferenceValue?.GetType() ?? owner;
            }
            if (parent.isArray && parent.propertyType != SerializedPropertyType.String)
            {
                var elementType = owner == null ? null : TypeUtils.GetElementType(owner);
                for (var i = 0; i < parent.arraySize; i++)
                {
                    VisitField(parent.GetArrayElementAtIndex(i), elementType, null, visitor, depth, conditionalElements);
                }
                return;
            }
            ForEachChild(parent, child =>
            {
                var field = FindField(owner, child.name);
                VisitField(child, field?.FieldType, field, visitor, depth);
            });
        }

        private static void VisitField(SerializedProperty property, Type type, FieldInfo field, Visitor visitor, int depth, bool conditionalElement = false)
        {
            var condition = field?.GetCustomAttribute<ConditionalShowAttribute>();
            if (condition != null && !ConditionalShowPropertyDrawer.IsVisible(property, condition)) return;

            var access = Access.Inherited;
            if (BuildValueDrawer.Handles(type))
            {
                access = Access.Override;
            }
            else if ((property.isArray || condition != null || conditionalElement) && property.hasVisibleChildren &&
                property.propertyType is SerializedPropertyType.Generic or SerializedPropertyType.ManagedReference)
            {
                // Unity's list drawer owns its children, and forwards ConditionalShow to each element.
                // Draw just the foldout here so each child gets its own Development access rules.
                access = Access.Foldout;
            }
            if (visitor(property, field, access, depth) && access != Access.Override)
            {
                Visit(property, type, visitor, depth + 1, condition != null && property.isArray);
            }
        }

        private static FieldInfo FindField(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (; type != null; type = type.BaseType)
            {
                var field = type.GetField(name, flags);
                if (field != null) return field;
            }
            return null;
        }

        internal static void ForEachChild(SerializedProperty parent, Action<SerializedProperty> visit)
        {
            using var child = parent.Copy();
            using var end = parent.GetEndProperty();
            var next = child.NextVisible(true);
            while (next && !SerializedProperty.EqualContents(child, end))
            {
                visit(child);
                next = child.NextVisible(false);
            }
        }
    }
}
