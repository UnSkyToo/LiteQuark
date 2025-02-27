using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    [CustomEditor(typeof(LiteAgentConfig))]
    public class LiteAgentConfigEditor : LiteScriptableObjectBaseEditor
    {
        protected override void OnDraw()
        {
            LiteEditorLayoutDrawer.DrawObject(new GUIContent("Agent Config"), target, LitePropertyType.Object);
        }
    }
}