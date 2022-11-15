using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomEditor(typeof(LiteLauncher))]
    public sealed class LiteLauncherEditor : UnityEditor.Editor
    {
        private SerializedProperty LogicClassNameProperty_;
        private SerializedProperty AssetModeProperty_;
        private SerializedProperty TargetFrameRateProperty_;
        private SerializedProperty MultiTouchProperty_;

        private SerializedProperty AutoRestartInBackgroundProperty_;
        private SerializedProperty BackgroundLimitTimeProperty_;

        private void OnEnable()
        {
            LogicClassNameProperty_ = serializedObject.FindProperty("LogicClassName");
            AssetModeProperty_ = serializedObject.FindProperty("AssetMode");
            TargetFrameRateProperty_ = serializedObject.FindProperty("TargetFrameRate");
            MultiTouchProperty_ = serializedObject.FindProperty("MultiTouch");

            AutoRestartInBackgroundProperty_ = serializedObject.FindProperty("AutoRestartInBackground");
            BackgroundLimitTimeProperty_ = serializedObject.FindProperty("BackgroundLimitTime");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var typeList = GetLogicTypeList();
            var selectIndex = -1;
            for (var index = 0; index < typeList.Length; ++index)
            {
                if (LogicClassNameProperty_.stringValue == typeList[index])
                {
                    selectIndex = index;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            selectIndex = EditorGUILayout.Popup(new GUIContent("Logic Entry"), selectIndex, typeList);
            if (EditorGUI.EndChangeCheck())
            {
                LogicClassNameProperty_.stringValue = typeList[selectIndex];
            }
            
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

        private string[] GetLogicTypeList()
        {
            var typeList = TypeCache.GetTypesDerivedFrom(typeof(IGameLogic));
            var nameList = new List<string>();

            foreach (var type in typeList)
            {
                nameList.Add($"{type.FullName}|{type.Assembly.FullName}");
            }

            return nameList.ToArray();
        }
        
        private void DrawSubProperty(SerializedProperty property)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(property);
            EditorGUI.indentLevel--;
        }
    }
}