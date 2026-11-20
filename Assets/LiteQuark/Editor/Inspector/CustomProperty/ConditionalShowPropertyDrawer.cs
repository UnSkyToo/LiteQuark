using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomPropertyDrawer(typeof(ConditionalShowAttribute))]
    internal class ConditionalShowPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsVisible(property, (ConditionalShowAttribute)attribute))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return IsVisible(property, (ConditionalShowAttribute)attribute)
                ? EditorGUI.GetPropertyHeight(property, label)
                : -EditorGUIUtility.standardVerticalSpacing;
        }

        internal static bool IsVisible(SerializedProperty property, ConditionalShowAttribute condition)
        {
            var path = property.propertyPath;
            var parent = path.Substring(0, path.LastIndexOf('.') + 1);
            for (var i = 0; i < condition.SourceFields.Length; i++)
            {
                var name = condition.SourceFields[i];
                var source = property.serializedObject.FindProperty(parent + name) ?? property.serializedObject.FindProperty(name);
                if (source == null) continue;

                var matches = CheckPropertyType(source, i < condition.Values.Length ? condition.Values[i] : null);
                var inverse = i < condition.Inverses.Length && condition.Inverses[i];
                if (matches == inverse) return false;
            }
            return true;
        }

        private static bool CheckPropertyType(SerializedProperty sourcePropertyValue, object value)
        {
            switch (sourcePropertyValue.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return sourcePropertyValue.boolValue == (bool)value;
                case SerializedPropertyType.Enum:
                    return sourcePropertyValue.enumValueFlag == (int)value;
                case SerializedPropertyType.String:
                    return sourcePropertyValue.stringValue == (string)value;
                case SerializedPropertyType.ObjectReference:
                    return sourcePropertyValue.objectReferenceValue != null;
                default:
                    Debug.LogError("Data type of the property used for conditional hiding [" + sourcePropertyValue.propertyType + "] is currently not supported");
                    return true;
            }
        }
    }
}
