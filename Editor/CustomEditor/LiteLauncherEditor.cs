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
        
        private void OnEnable()
        {
            LogicClassNameProperty_ = serializedObject.FindProperty("LogicClassName");
            AssetModeProperty_ = serializedObject.FindProperty("AssetMode");
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
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUI.BeginChangeCheck();
            var modeIndex = EditorGUILayout.Popup(new GUIContent("Asset Mode"), AssetModeProperty_.enumValueIndex, AssetModeProperty_.enumNames);
            if (EditorGUI.EndChangeCheck())
            {
                AssetModeProperty_.enumValueIndex = modeIndex;
                serializedObject.ApplyModifiedProperties();
            }
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
    }
}