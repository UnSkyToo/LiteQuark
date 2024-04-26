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
            var assemblyNameProperty = property.FindPropertyRelative("AssemblyName");
            var typeNameProperty = property.FindPropertyRelative("TypeName");
            
            var typeList = GetLogicTypeList();
            var selectIndex = Array.FindIndex(typeList, s => s == $"{typeNameProperty.stringValue}|{assemblyNameProperty.stringValue}");
            
            var disabledRect = new Rect(position.x, position.y, position.height, position.height);
            disabledProperty.boolValue = !EditorGUI.Toggle(disabledRect, !disabledProperty.boolValue);
            
            EditorGUI.BeginDisabledGroup(disabledProperty.boolValue);
            
            var popupRect = new Rect(position.x + disabledRect.width, position.y, position.width - disabledRect.width, position.height);
            EditorGUI.BeginChangeCheck();
            selectIndex = EditorGUI.Popup(popupRect, selectIndex, typeList);
            if (EditorGUI.EndChangeCheck())
            {
                var chunks = typeList[selectIndex].Split('|');
                if (chunks.Length == 2)
                {
                    assemblyNameProperty.stringValue = chunks[1];
                    typeNameProperty.stringValue = chunks[0];
                }
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
                nameList.Add($"{type.FullName}|{type.Assembly.FullName}");
            }

            return nameList.ToArray();
        }
    }
}