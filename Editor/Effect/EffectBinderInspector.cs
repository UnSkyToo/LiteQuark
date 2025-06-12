using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
	[CustomEditor(typeof(EffectBinder))]
    internal class EffectBinderInspector : LiteInspectorBaseView
    {
        protected override void OnDraw()
        {
            if (Application.isPlaying)
            {
                return;
            }
            
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");

            if (GUILayout.Button("Update"))
            {
                (target as EffectBinder)?.UpdateInfo();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
	}
}