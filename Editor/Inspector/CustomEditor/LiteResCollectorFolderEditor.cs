using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomEditor(typeof(DefaultAsset))]
    public class LiteResCollectorFolderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (Application.isPlaying)
            {
                return;
            }
            
            var path = AssetDatabase.GetAssetPath(target);
            if (AssetDatabase.IsValidFolder(path) && path != LiteConst.AssetRootPath && path.StartsWith(LiteConst.AssetRootPath))
            {
                var previousEnabled = GUI.enabled;
                GUI.enabled = true;
                DrawFolder(path);
                GUI.enabled = previousEnabled;
            }
        }

        private void DrawFolder(string path)
        {
            EditorGUI.BeginChangeCheck();
            var setting = ResCollectorSetting.GetOrCreateSetting();
            var ignore = !EditorGUILayout.Toggle("Individual Bundle", !setting.IsIgnorePath(path));
            if (EditorGUI.EndChangeCheck())
            {
                setting.SetIgnorePath(path, ignore);
                EditorUtility.SetDirty(setting);
                AssetDatabase.SaveAssetIfDirty(setting);
            }
        }
    }
}