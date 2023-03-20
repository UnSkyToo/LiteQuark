using UnityEditor;
using UnityEngine;

namespace InfiniteGame
{
    public sealed class LabelAttribute : PropertyAttribute
    {
        public string Name { get; }

        public LabelAttribute(string name)
        {
            Name = name;
        }
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public sealed class LabelPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            LabelAttribute attr = attribute as LabelAttribute;
            if(attr.Name.Length > 0)
            {
                label.text = attr.Name;
            }
            EditorGUI.PropertyField(position, property, label);
        }
    }
#endif
}