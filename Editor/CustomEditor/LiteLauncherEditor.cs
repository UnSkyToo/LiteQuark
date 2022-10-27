using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomEditor(typeof(LiteLauncher))]
    public sealed class LiteLauncherEditor : UnityEditor.Editor
    {
        private SerializedProperty LogicNameProperty_;
        
        private void OnEnable()
        {
            LogicNameProperty_ = serializedObject.FindProperty("LogicClassName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var typeList = GetLogicTypeList();
            var selectIndex = -1;
            for (var index = 0; index < typeList.Length; ++index)
            {
                if (LogicNameProperty_.stringValue == typeList[index])
                {
                    selectIndex = index;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            selectIndex = EditorGUILayout.Popup(new GUIContent("Logic Entry"), selectIndex, typeList);
            if (EditorGUI.EndChangeCheck())
            {
                LogicNameProperty_.stringValue = typeList[selectIndex];
                serializedObject.ApplyModifiedProperties();
            }
        }

        private string[] GetLogicTypeList()
        {
            var typeList = TypeCache.GetTypesDerivedFrom(typeof(IGameLogic));
            var nameList = new List<string>();

            foreach (var type in typeList)
            {
                nameList.Add(type.FullName);
            }

            return nameList.ToArray();
        }
    }
}