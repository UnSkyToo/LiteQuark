using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomEditor(typeof(LiteDebugger))]
    internal class LiteLauncherInspectorView : LiteInspectorBaseView
    {
        private bool AssetFoldout_ = false;
        private bool ObjectPoolFoldout_ = false;
        
        protected override void OnDraw()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            DrawFoldout(ref AssetFoldout_, "Asset", DrawAsset);
            DrawFoldout(ref ObjectPoolFoldout_, "ObjectPool", DrawObjectPool);
        }

        private void DrawFoldout(ref bool foldout, string label, System.Action func)
        {
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, label);
            if (foldout)
            {
                func.Invoke();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawAsset()
        {
            var system = LiteRuntime.Asset;
        }

        private void DrawObjectPool()
        {
            var cache = LiteRuntime.ObjectPool.GetPoolCache();
            if (cache.Count == 0)
            {
                EditorGUILayout.LabelField("Empty");
                return;
            }

            EditorGUILayout.LabelField("Name", "Count\tActive\tInactive");
            foreach (var chunk in cache)
            {
                EditorGUILayout.LabelField($"{chunk.Value.Name}", $"{chunk.Value.CountAll}\t{chunk.Value.CountActive}\t{chunk.Value.CountInactive}");
            }
        }
    }
}