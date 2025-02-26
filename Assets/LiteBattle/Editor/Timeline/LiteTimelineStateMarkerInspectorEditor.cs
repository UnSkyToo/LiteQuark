using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    [CustomEditor(typeof(LiteTimelineStateMarker))]
    internal sealed class LiteTimelineStateMarkerInspectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (!LiteEditorBinder.Instance.IsReady)
            {
                return;
            }
            
            EditorGUI.BeginChangeCheck();
            var data = target as LiteTimelineStateMarker;
            LiteEditorLayoutDrawer.DrawObject(GUIContent.none, data, LitePropertyType.Object);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}