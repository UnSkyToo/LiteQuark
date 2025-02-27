using UnityEditor;

namespace LiteQuark.Editor
{
    public abstract class LiteScriptableObjectBaseEditor : UnityEditor.Editor
    {
        public sealed override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            
            if (target is UnityEngine.ScriptableObject)
            {
                serializedObject.Update();
                
                EditorGUI.BeginChangeCheck();
                OnDraw();
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(target);
                    AssetDatabase.SaveAssetIfDirty(target);
                }
            }
            else if (target is not null)
            {
                LiteEditorLayoutDrawer.LabelError($"{target.GetType()} is not ScriptableObject");
            }
        }

        private void OnDisable()
        {
            AssetDatabase.SaveAssetIfDirty(target);
        }

        protected abstract void OnDraw();
    }
}