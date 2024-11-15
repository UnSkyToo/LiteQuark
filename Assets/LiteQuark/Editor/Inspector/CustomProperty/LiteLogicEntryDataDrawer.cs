using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomPropertyDrawer(typeof(LiteLogicEntryData))]
    public class LiteLogicEntryDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var disabledProperty = property.FindPropertyRelative("Disabled");
            var assemblyQualifiedNameProperty = property.FindPropertyRelative("AssemblyQualifiedName");
            
            var typeList = GetLogicTypeList();
            var selectIndex = Array.FindIndex(typeList, s => s == assemblyQualifiedNameProperty.stringValue);
            
            var disabledRect = new Rect(position.x, position.y, position.height, position.height);
            disabledProperty.boolValue = !EditorGUI.Toggle(disabledRect, !disabledProperty.boolValue);
            
            EditorGUI.BeginDisabledGroup(disabledProperty.boolValue);
            
            var popupRect = new Rect(position.x + disabledRect.width, position.y, position.width - disabledRect.width, position.height);
            EditorGUI.BeginChangeCheck();
            selectIndex = EditorGUI.Popup(popupRect, selectIndex, typeList);
            if (EditorGUI.EndChangeCheck())
            {
                assemblyQualifiedNameProperty.stringValue = typeList[selectIndex];
            }
            
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndProperty();
        }
        
        private string[] GetLogicTypeList()
        {
            var typeList = TypeCache.GetTypesDerivedFrom(typeof(ILogic));
            var nameList = new List<string>();

            foreach (var type in typeList)
            {
                nameList.Add(type.AssemblyQualifiedName);
            }

            return nameList.ToArray();
        }
    }
}