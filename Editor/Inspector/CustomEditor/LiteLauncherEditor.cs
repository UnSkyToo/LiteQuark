// using LiteQuark.Runtime;
// using UnityEditor;
//
// namespace LiteQuark.Editor
// {
//     [CustomEditor(typeof(LiteLauncher))]
//     public sealed class LiteLauncherEditor : UnityEditor.Editor
//     {
//         private SerializedProperty SettingProperty_;
//         
//         private SerializedProperty LogicListProperty_;
//         private SerializedProperty AssetModeProperty_;
//         private SerializedProperty TargetFrameRateProperty_;
//         private SerializedProperty MultiTouchProperty_;
//
//         private SerializedProperty AutoRestartInBackgroundProperty_;
//         private SerializedProperty BackgroundLimitTimeProperty_;
//
//         private SerializedProperty ReceiveLogProperty_;
//         private SerializedProperty LogInfoProperty_;
//         private SerializedProperty LogWarnProperty_;
//         private SerializedProperty LogErrorProperty_;
//         private SerializedProperty LogFatalProperty_;
//         private SerializedProperty ShowLogViewerProperty_;
//
//         private SerializedProperty ResolutionWidthProperty_;
//         private SerializedProperty ResolutionHeightProperty_;
//
//         private void OnEnable()
//         {
//             SettingProperty_ = serializedObject.FindProperty("Setting");
//             
//             LogicListProperty_ = serializedObject.FindProperty("LogicList");
//             AssetModeProperty_ = serializedObject.FindProperty("AssetMode");
//             TargetFrameRateProperty_ = serializedObject.FindProperty("TargetFrameRate");
//             MultiTouchProperty_ = serializedObject.FindProperty("MultiTouch");
//
//             AutoRestartInBackgroundProperty_ = serializedObject.FindProperty("AutoRestartInBackground");
//             BackgroundLimitTimeProperty_ = serializedObject.FindProperty("BackgroundLimitTime");
//
//             ReceiveLogProperty_ = serializedObject.FindProperty("ReceiveLog");
//             LogInfoProperty_ = serializedObject.FindProperty("LogInfo");
//             LogWarnProperty_ = serializedObject.FindProperty("LogWarn");
//             LogErrorProperty_ = serializedObject.FindProperty("LogError");
//             LogFatalProperty_ = serializedObject.FindProperty("LogFatal");
//             ShowLogViewerProperty_ = serializedObject.FindProperty("ShowLogViewer");
//
//             ResolutionWidthProperty_ = serializedObject.FindProperty("ResolutionWidth");
//             ResolutionHeightProperty_ = serializedObject.FindProperty("ResolutionHeight");
//         }
//
//         public override void OnInspectorGUI()
//         {
//             serializedObject.Update();
//             
//             DrawProperty(SettingProperty_);
//             
//             DrawProperty(LogicListProperty_);
//             
//             DrawProperty(AssetModeProperty_);
//             
//             DrawProperty(TargetFrameRateProperty_);
//
//             DrawProperty(MultiTouchProperty_);
//             
//             DrawProperty(AutoRestartInBackgroundProperty_);
//             if(AutoRestartInBackgroundProperty_.boolValue)
//             {
//                 DrawSubProperty(BackgroundLimitTimeProperty_);
//             }
//
//             DrawProperty(ReceiveLogProperty_);
//             if (ReceiveLogProperty_.boolValue)
//             {
//                 DrawSubProperty(LogInfoProperty_);
//                 DrawSubProperty(LogWarnProperty_);
//                 DrawSubProperty(LogErrorProperty_);
//                 DrawSubProperty(LogFatalProperty_);
//                 DrawSubProperty(ShowLogViewerProperty_);
//             }
//             
//             DrawProperty(ResolutionWidthProperty_);
//             DrawProperty(ResolutionHeightProperty_);
//
//             serializedObject.ApplyModifiedProperties();
//
//             // DrawPropertiesExcluding(serializedObject, "m_Script");
//         }
//
//         private void DrawProperty(SerializedProperty property)
//         {
//             EditorGUILayout.PropertyField(property);
//         }
//
//         private void DrawSubProperty(SerializedProperty property)
//         {
//             EditorGUI.indentLevel++;
//             EditorGUILayout.PropertyField(property);
//             EditorGUI.indentLevel--;
//         }
//     }
// }