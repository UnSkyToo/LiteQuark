﻿using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomEditor(typeof(LiteDebugger))]
    internal class LiteDebuggerInspectorView : LiteInspectorBaseView
    {
        private Dictionary<string, bool> LabelFoldout_ = new Dictionary<string, bool>();
        
        protected override void OnDraw()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            
            DrawFoldout("Action", DrawAction);
            DrawFoldout("Asset", DrawAsset);
            DrawFoldout("ObjectPool", DrawObjectPool);
        }

        private void DrawFoldout(string label, System.Action func)
        {
            LabelFoldout_.TryAdd(label, false);
            
            LabelFoldout_[label] = EditorGUILayout.Foldout(LabelFoldout_[label], label);
            if (LabelFoldout_[label])
            {
                func.Invoke();
            }
        }

        private void DrawAction()
        {
            var actionList = LiteRuntime.Action?.GetActionList();
            if (actionList == null || actionList.Count == 0)
            {
                EditorGUILayout.LabelField("Empty");
                return;
            }

            actionList.Foreach((ac) =>
            {
                if (ac is BaseAction action)
                {
                    EditorGUILayout.LabelField(action.DebugName);
                }
                else
                {
                    EditorGUILayout.LabelField($"Action {ac.ID}");
                }
            });
        }

        private void DrawAsset()
        {
            var visitorInfoWrap = LiteRuntime.Asset?.GetVisitorInfo();
            if (!visitorInfoWrap.HasValue || visitorInfoWrap.Value.Tag == null)
            {
                EditorGUILayout.LabelField("Empty");
                return;
            }
            
            var visitorInfo = visitorInfoWrap.Value;
            if (visitorInfo.BundleVisitorList.Count == 0)
            {
                EditorGUILayout.LabelField("Empty");
                return;
            }

            foreach (var bundleInfo in visitorInfo.BundleVisitorList)
            {
                EditorGUILayout.LabelField(bundleInfo.BundlePath, $"{bundleInfo.RefCount} {bundleInfo.Stage}({bundleInfo.RetainTime:0.0}s)");

                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var assetInfo in bundleInfo.AssetVisitorList)
                    {
                        EditorGUILayout.LabelField(assetInfo.AssetPath, $"{assetInfo.RefCount} {assetInfo.Stage}({assetInfo.RetainTime:0.0}s)");
                    }
                }
            }
        }

        private void DrawObjectPool()
        {
            var cache = LiteRuntime.ObjectPool?.GetPoolCache();
            if (cache == null || cache.Count == 0)
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