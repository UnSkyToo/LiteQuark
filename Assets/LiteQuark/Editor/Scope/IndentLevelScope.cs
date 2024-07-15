using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public sealed class IndentLevelScope : GUI.Scope
    {
        public int Level { get; }

        public IndentLevelScope(int level = 1)
        {
            Level = level;
            EditorGUI.indentLevel += Level;
        }

        protected override void CloseScope()
        {
            EditorGUI.indentLevel -= Level;
        }
    }
}