using System;
using System.Reflection;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomPropertyDrawer(typeof(BuildValue<>))]
    internal sealed class BuildValueDrawer : PropertyDrawer
    {
        private static readonly GUIContent OverrideLabel = new("Override", "Use a separate Development value for this field.");

        internal static bool Handles(Type type)
        {
            return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(BuildValue<>);
        }

        private static LiteBuildProfile? Profile(SerializedProperty property)
        {
            if (property.serializedObject.targetObject is LiteLauncher)
            {
                return (LiteBuildProfile)property.serializedObject.FindProperty(nameof(LiteLauncher.EditorPreview)).enumValueIndex;
            }
            return null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var profile = Profile(property);
            var release = property.FindPropertyRelative("Release");
            var releaseHeight = EditorGUI.GetPropertyHeight(release, GUIContent.none, true);
            if (profile == LiteBuildProfile.Release) return releaseHeight;

            var height = DevelopmentHeight(property);
            // A BuildValue used outside a Launcher has no environment selector; expose both values there.
            return profile.HasValue ? height : releaseHeight + EditorGUIUtility.standardVerticalSpacing + height;
        }

        private static float DevelopmentHeight(SerializedProperty property)
        {
            var value = ValueFor(property, LiteBuildProfile.Development, out _);
            var height = EditorGUI.GetPropertyHeight(value, GUIContent.none, true);
            if (EditorGUIUtility.currentViewWidth < 380f)
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return height;
        }

        internal static void DrawDevelopmentLayout(SerializedProperty property, FieldInfo field)
        {
            var title = field?.GetCustomAttribute<InspectorNameAttribute>()?.displayName ?? property.displayName;
            DrawDevelopment(EditorGUILayout.GetControlRect(true, DevelopmentHeight(property)), property, new GUIContent(title, property.tooltip));
        }

        internal static SerializedProperty ValueFor(SerializedProperty property, LiteBuildProfile profile, out bool editable)
        {
            var useOverride = profile == LiteBuildProfile.Development && property.FindPropertyRelative("OverrideInDevelopment").boolValue;
            editable = profile == LiteBuildProfile.Release || useOverride;
            return property.FindPropertyRelative(useOverride ? "Development" : "Release");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var profile = Profile(property);
            var title = fieldInfo?.GetCustomAttribute<InspectorNameAttribute>()?.displayName ?? label.text;
            if (profile != LiteBuildProfile.Development)
            {
                var release = ValueFor(property, LiteBuildProfile.Release, out _);
                var row = position;
                row.height = EditorGUI.GetPropertyHeight(release, GUIContent.none, true);
                EditorGUI.PropertyField(row, release, new GUIContent(profile.HasValue ? title : title + " (Release)", label.tooltip), true);
                if (profile.HasValue) return;
                position.yMin = row.yMax + EditorGUIUtility.standardVerticalSpacing;
            }

            DrawDevelopment(position, property, new GUIContent(profile.HasValue ? title : "Development", label.tooltip));
        }

        private static void DrawDevelopment(Rect position, SerializedProperty property, GUIContent label)
        {
            var toggle = property.FindPropertyRelative("OverrideInDevelopment");
            var header = position;
            header.height = EditorGUIUtility.singleLineHeight;
            var controls = EditorGUI.PrefixLabel(header, label);
            var checkbox = controls;
            checkbox.xMin = checkbox.xMax - 76f;
            var valueRect = controls;
            if (EditorGUIUtility.currentViewWidth < 380f)
            {
                valueRect = EditorGUI.IndentedRect(position);
                valueRect.yMin = header.yMax + EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                valueRect.xMax = checkbox.xMin - EditorGUIUtility.standardVerticalSpacing;
            }

            using var indent = new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel);
            using (var scope = new EditorGUI.PropertyScope(checkbox, OverrideLabel, toggle))
            {
                EditorGUI.BeginChangeCheck();
                var enabled = EditorGUI.ToggleLeft(checkbox, scope.content, toggle.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    GUI.FocusControl(null);
                    EditorGUIUtility.editingTextField = false;
                    toggle.boolValue = enabled;
                }
            }
            var value = ValueFor(property, LiteBuildProfile.Development, out var editable);
            valueRect.height = EditorGUI.GetPropertyHeight(value, GUIContent.none, true);
            using (new EditorGUI.DisabledScope(!editable))
            {
                EditorGUI.PropertyField(valueRect, value, GUIContent.none, true);
            }
        }
    }
}
