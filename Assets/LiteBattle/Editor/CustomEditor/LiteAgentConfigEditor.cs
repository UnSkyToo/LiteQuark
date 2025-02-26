using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    [CustomEditor(typeof(LiteAgentConfig))]
    public class LiteAgentConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            LiteEditorLayoutDrawer.DrawObject(new GUIContent("Agent Config"), target, LitePropertyType.Object);
        }
    }
}