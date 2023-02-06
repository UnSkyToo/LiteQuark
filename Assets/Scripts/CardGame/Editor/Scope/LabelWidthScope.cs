using UnityEditor;
using UnityEngine;

namespace LiteEditor
{
    public sealed class LabelWidthScope : GUI.Scope
    {
      public float LabelWidth { get; }

      public LabelWidthScope(float labelWidth)
      {
          LabelWidth = EditorGUIUtility.labelWidth;
          EditorGUIUtility.labelWidth = labelWidth;
      }

      protected override void CloseScope()
      {
          EditorGUIUtility.labelWidth = LabelWidth;
      }
    }
}