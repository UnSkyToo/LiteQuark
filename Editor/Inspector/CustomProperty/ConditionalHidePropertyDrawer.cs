using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
    public class ConditionalHidePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var condHAtt = attribute as ConditionalHideAttribute;
            var enabled = GetConditionalHideAttributeResult(condHAtt, property);

            var wasEnabled = GUI.enabled;
            GUI.enabled = enabled;
            if (enabled)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            GUI.enabled = wasEnabled;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var condHAtt = attribute as ConditionalHideAttribute;
            var enabled = GetConditionalHideAttributeResult(condHAtt, property);

            if (enabled)
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            else
            {
                //The property is not being drawn
                //We want to undo the spacing added before and after the property
                return -EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private bool GetConditionalHideAttributeResult(ConditionalHideAttribute condHAtt, SerializedProperty property)
        {
            var enabled = true;

            //Handle the unlimited property array
            var sourceFieldArray = condHAtt.SourceFields;
            var valueArray = condHAtt.Values;
            var inverseArray = condHAtt.Inverses;
            for (var index = 0; index < sourceFieldArray.Length; ++index)
            {
                SerializedProperty sourcePropertyValueFromArray = null;
                if (!property.isArray || property.propertyType == SerializedPropertyType.String)
                {
                    var propertyPath = property.propertyPath; //returns the property path of the property we want to apply the attribute to
                    var conditionPath = propertyPath.Replace(property.name, sourceFieldArray[index]); //changes the path to the conditionalsource property path
                    sourcePropertyValueFromArray = property.serializedObject.FindProperty(conditionPath);

                    //if the find failed->fall back to the old system
                    if (sourcePropertyValueFromArray == null)
                    {
                        //original implementation (doens't work with nested serializedObjects)
                        sourcePropertyValueFromArray = property.serializedObject.FindProperty(sourceFieldArray[index]);
                    }
                }
                else
                {
                    // original implementation(doens't work with nested serializedObjects) 
                    sourcePropertyValueFromArray = property.serializedObject.FindProperty(sourceFieldArray[index]);
                }

                //Combine the results
                if (sourcePropertyValueFromArray != null)
                {
                    var propertyEnabled = CheckPropertyType(sourcePropertyValueFromArray, valueArray.Length >= (index + 1) ? valueArray[index] : null);
                    if (inverseArray.Length >= (index + 1) && inverseArray[index]) propertyEnabled = !propertyEnabled;

                    enabled = enabled && propertyEnabled;
                }
                else
                {
                    //Debug.LogWarning("Attempting to use a ConditionalHideAttribute but no matching SourcePropertyValue found in object: " + condHAtt.ConditionalSourceField);
                }
            }

            return enabled;
        }

        private bool CheckPropertyType(SerializedProperty sourcePropertyValue, object value)
        {
            switch (sourcePropertyValue.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return sourcePropertyValue.boolValue == (bool)value;
                case SerializedPropertyType.Enum:
                    return sourcePropertyValue.enumValueFlag == (int)value;
                case SerializedPropertyType.String:
                    return sourcePropertyValue.stringValue == (string)value;
                case SerializedPropertyType.ObjectReference:
                    return sourcePropertyValue.objectReferenceValue != null;
                default:
                    Debug.LogError("Data type of the property used for conditional hiding [" + sourcePropertyValue.propertyType + "] is currently not supported");
                    return true;
            }
        }
    }
}