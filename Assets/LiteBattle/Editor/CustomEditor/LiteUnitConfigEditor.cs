using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    [CustomEditor(typeof(LiteUnitConfig))]
    public class LiteUnitConfigEditor : LiteScriptableObjectBaseEditor
    {
        protected override void OnDraw()
        {
            LiteEditorLayoutDrawer.DrawObject(new GUIContent("Unit Config"), target, LitePropertyType.Object);
        }
    }
}