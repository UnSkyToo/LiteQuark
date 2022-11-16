using LiteQuark.Runtime;
using UnityEditor;

namespace LiteQuark.Editor
{
    [CustomEditor(typeof(LiteLauncher))]
    public sealed class LiteLauncherEditor : UnityEditor.Editor
    {
        private SerializedProperty LogicListProperty_;
        private SerializedProperty AssetModeProperty_;
        private SerializedProperty TargetFrameRateProperty_;
        private SerializedProperty MultiTouchProperty_;

        private SerializedProperty AutoRestartInBackgroundProperty_;
        private SerializedProperty BackgroundLimitTimeProperty_;

        private void OnEnable()
        {
            LogicListProperty_ = serializedObject.FindProperty("LogicList");
            AssetModeProperty_ = serializedObject.FindProperty("AssetMode");
            TargetFrameRateProperty_ = serializedObject.FindProperty("TargetFrameRate");
            MultiTouchProperty_ = serializedObject.FindProperty("MultiTouch");

            AutoRestartInBackgroundProperty_ = serializedObject.FindProperty("AutoRestartInBackground");
            BackgroundLimitTimeProperty_ = serializedObject.FindProperty("BackgroundLimitTime");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(LogicListProperty_);
            
            EditorGUILayout.PropertyField(AssetModeProperty_);
            
            EditorGUILayout.PropertyField(TargetFrameRateProperty_);

            EditorGUILayout.PropertyField(MultiTouchProperty_);
            
            EditorGUILayout.PropertyField(AutoRestartInBackgroundProperty_);
            if(AutoRestartInBackgroundProperty_.boolValue)
            {
                DrawSubProperty(BackgroundLimitTimeProperty_);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSubProperty(SerializedProperty property)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(property);
            EditorGUI.indentLevel--;
        }
    }
}