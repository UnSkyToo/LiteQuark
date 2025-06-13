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
                if (target is EffectBinder binder)
                {
                    Undo.RecordObject(binder, "Update Effect Info");
                    binder.UpdateInfo();
                    EditorUtility.SetDirty(binder);
                    AssetDatabase.SaveAssetIfDirty(binder);
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
	}
}