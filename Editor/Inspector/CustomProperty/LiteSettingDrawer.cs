using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    // [CustomPropertyDrawer(typeof(LiteSetting))]
    // public class LiteSettingDrawer : PropertyDrawer
    // {
    //     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //     {
    //         EditorGUI.BeginProperty(position, label, property);
    //         EditorGUILayout.PropertyField(property.FindPropertyRelative("LogicList"));
    //         EditorGUILayout.PropertyField(property.FindPropertyRelative("Common"));
    //         EditorGUILayout.PropertyField(property.FindPropertyRelative("Asset"));
    //         EditorGUILayout.PropertyField(property.FindPropertyRelative("Log"));
    //         EditorGUILayout.PropertyField(property.FindPropertyRelative("UI"));
    //         EditorGUI.EndProperty();
    //     }
    // }
    //
    // [CustomPropertyDrawer(typeof(LiteSetting.CommonSetting))]
    // public class LiteCommonSettingDrawer : PropertyDrawer
    // {
    //     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //     {
    //         EditorGUI.BeginProperty(position, label, property);
    //         EditorGUILayout.PropertyField(property);
    //         EditorGUI.EndProperty();
    //     }
    // }
    //
    // [CustomPropertyDrawer(typeof(LiteSetting.AssetSetting))]
    // public class LiteAssetSettingDrawer : PropertyDrawer
    // {
    //     private bool Foldout_ = false;
    //     
    //     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //     {
    //         EditorGUI.BeginProperty(position, label, property);
    //         Foldout_ = EditorGUILayout.BeginFoldoutHeaderGroup(Foldout_, "Asset");
    //         
    //         using (new EditorGUI.IndentLevelScope(2))
    //         {
    //             if (Foldout_)
    //             {
    //                 EditorGUILayout.PropertyField(property.FindPropertyRelative("AssetMode"));
    //
    //                 var enableRetainProperty = property.FindPropertyRelative("EnableRetain");
    //                 EditorGUILayout.PropertyField(enableRetainProperty);
    //
    //                 if (enableRetainProperty.boolValue)
    //                 {
    //                     using (new EditorGUI.IndentLevelScope())
    //                     {
    //                         EditorGUILayout.PropertyField(property.FindPropertyRelative("AssetRetainTime"));
    //                         EditorGUILayout.PropertyField(property.FindPropertyRelative("BundleRetainTime"));
    //                     }
    //                 }
    //             }
    //         }
    //
    //         EditorGUILayout.EndFoldoutHeaderGroup();
    //         EditorGUI.EndProperty();
    //     }
    // }
    //
    // [CustomPropertyDrawer(typeof(LiteSetting.ActionSetting))]
    // public class LiteActionSettingDrawer : PropertyDrawer
    // {
    //     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //     {
    //         EditorGUI.BeginProperty(position, label, property);
    //         EditorGUILayout.PropertyField(property);
    //         EditorGUI.EndProperty();
    //     }
    // }
    //
    // [CustomPropertyDrawer(typeof(LiteSetting.LogSetting))]
    // public class LiteLogSettingDrawer : PropertyDrawer
    // {
    //     private bool Foldout_ = false;
    //     
    //     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //     {
    //         EditorGUI.BeginProperty(position, label, property);
    //         Foldout_ = EditorGUILayout.BeginFoldoutHeaderGroup(Foldout_, "Log");
    //
    //         using (new EditorGUI.IndentLevelScope(2))
    //         {
    //             if (Foldout_)
    //             {
    //                 EditorGUILayout.PropertyField(property.FindPropertyRelative("SimpleLog"));
    //                 
    //                 var receiveLogProperty = property.FindPropertyRelative("ReceiveLog");
    //                 EditorGUILayout.PropertyField(receiveLogProperty);
    //
    //                 if (receiveLogProperty.boolValue)
    //                 {
    //                     using (new EditorGUI.IndentLevelScope())
    //                     {
    //                         EditorGUILayout.PropertyField(property.FindPropertyRelative("LogInfo"));
    //                         EditorGUILayout.PropertyField(property.FindPropertyRelative("LogWarn"));
    //                         EditorGUILayout.PropertyField(property.FindPropertyRelative("LogError"));
    //                         EditorGUILayout.PropertyField(property.FindPropertyRelative("LogFatal"));
    //                         EditorGUILayout.PropertyField(property.FindPropertyRelative("ShowLogViewer"));
    //                     }
    //                 }
    //             }
    //         }
    //
    //         EditorGUILayout.EndFoldoutHeaderGroup();
    //         EditorGUI.EndProperty();
    //     }
    // }
    //
    // [CustomPropertyDrawer(typeof(LiteSetting.UISetting))]
    // public class LiteUISettingDrawer : PropertyDrawer
    // {
    //     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //     {
    //         EditorGUI.BeginProperty(position, label, property);
    //         EditorGUILayout.PropertyField(property);
    //         EditorGUI.EndProperty();
    //     }
    // }
}