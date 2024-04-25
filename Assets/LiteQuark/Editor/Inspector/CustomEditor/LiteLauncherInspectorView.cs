using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomEditor(typeof(LiteDebugger))]
    internal class LiteLauncherInspectorView : LiteInspectorBaseView
    {
        private bool AssetFoldout_ = true;
        private bool ObjectPoolFoldout_ = false;

        private Dictionary<string, bool> BundleFoldout_ = new Dictionary<string, bool>();
        
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
            foldout = EditorGUILayout.Foldout(foldout, label);
            if (foldout)
            {
                func.Invoke();
            }
        }

        private void DrawAsset()
        {
            var visitorInfo = LiteRuntime.Asset.GetVisitorInfo();
            if (visitorInfo.Tag == null)
            {
                return;
            }

            if (visitorInfo.BundleVisitorList.Count == 0)
            {
                EditorGUILayout.LabelField("Empty");
                return;
            }

            foreach (var bundleInfo in visitorInfo.BundleVisitorList)
            {
                EditorGUILayout.LabelField(bundleInfo.BundlePath, $"{bundleInfo.RefCount} {bundleInfo.IsLoaded} {bundleInfo.LoadTime}");

                EditorGUI.indentLevel++;
                foreach (var assetInfo in bundleInfo.AssetVisitorList)
                {
                    EditorGUILayout.LabelField(assetInfo.AssetPath, $"{assetInfo.RefCount} {assetInfo.IsLoaded} {assetInfo.LoadTime}");
                }
                EditorGUI.indentLevel--;
            }
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