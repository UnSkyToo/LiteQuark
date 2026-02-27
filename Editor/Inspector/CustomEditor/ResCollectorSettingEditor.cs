using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomEditor(typeof(ResCollectorSetting))]
    public sealed class ResCollectorSettingEditor : LiteScriptableObjectBaseEditor
    {
        protected override void OnDraw()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Clean Invalid Paths"))
            {
                CleanInvalidPaths();
                serializedObject.Update();
            }
        }

        private void CleanInvalidPaths()
        {
            var setting = target as ResCollectorSetting;
            if (setting == null)
            {
                return;
            }
            
            var removed = setting.IgnorePathList.RemoveAll(path =>
                string.IsNullOrWhiteSpace(path) || !AssetDatabase.IsValidFolder(path));
            
            if (removed > 0)
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
                LEditorLog.Info($"[ResCollectorSetting] Removed {removed} invalid path(s).");
            }
            else
            {
                LEditorLog.Info("[ResCollectorSetting] No invalid paths found.");
            }
        }
    }
}