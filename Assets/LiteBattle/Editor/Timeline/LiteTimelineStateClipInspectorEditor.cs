using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    [CustomEditor(typeof(LiteTimelineStateClip))]
    internal sealed class LiteStateClipInspectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {   
            base.OnInspectorGUI();

            if (!LiteEditorBinder.Instance.IsReady)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();
            var data = target as LiteTimelineStateClip;
            LiteEditorLayoutDrawer.DrawObject(GUIContent.none, data, LitePropertyType.Object);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}