using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public sealed class FieldWidthScope : GUI.Scope
    {
        public float FieldWidth { get; }

        public FieldWidthScope(float fieldWidth)
        {
            FieldWidth = EditorGUIUtility.fieldWidth;
            EditorGUIUtility.fieldWidth = fieldWidth;
        }

        protected override void CloseScope()
        {
            EditorGUIUtility.fieldWidth = FieldWidth;
        }
    }
}