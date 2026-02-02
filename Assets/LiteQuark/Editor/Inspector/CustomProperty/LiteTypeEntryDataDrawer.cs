using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomPropertyDrawer(typeof(LiteTypeEntryData<>))]
    internal class LiteTypeEntryDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var disabledProperty = property.FindPropertyRelative("Disabled");
            var assemblyQualifiedNameProperty = property.FindPropertyRelative("AssemblyQualifiedName");

            var baseType = GetBaseTypeFromFieldInfo();
            var typeList = GetTypeList(baseType);
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
        
        private Type GetBaseTypeFromFieldInfo()
        {
            var fieldType = fieldInfo.FieldType;
            var elementType = fieldType;
            
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                elementType = fieldType.GetGenericArguments()[0];
            }
            else if (fieldType.IsArray)
            {
                elementType = fieldType.GetElementType();
            }

            if (elementType == null)
            {
                return null;
            }
            
            if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(LiteTypeEntryData<>))
            {
                return elementType.GetGenericArguments()[0];
            }
            
            return null;
        }
        
        private string[] GetTypeList(Type baseType)
        {
            if (baseType == null)
            {
                return Array.Empty<string>();
            }
            
            var typeList = TypeCache.GetTypesDerivedFrom(baseType);
            var nameList = new List<string>();

            foreach (var type in typeList)
            {
                nameList.Add(type.AssemblyQualifiedName);
            }

            return nameList.ToArray();
        }
    }
}