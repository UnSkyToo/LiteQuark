using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomEditor(typeof(LiteDebugger))]
    internal class LiteDebuggerInspectorView : LiteInspectorBaseView
    {
        private readonly Dictionary<string, bool> _labelFoldout = new Dictionary<string, bool>();
        private readonly Dictionary<ulong, bool> _idFoldout = new Dictionary<ulong, bool>();
        
        protected override void OnDraw()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            
            DrawFoldout("Action", DrawAction);
            DrawFoldout("Asset", DrawAsset);
            DrawFoldout("Task", DrawTask);
            DrawFoldout("ObjectPool", DrawObjectPool);
        }

        private void DrawFoldout(string label, System.Action func)
        {
            _labelFoldout.TryAdd(label, false);
            
            _labelFoldout[label] = EditorGUILayout.Foldout(_labelFoldout[label], label);
            if (_labelFoldout[label])
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
            }
            else
            {
                actionList.Foreach(DrawOneAction, EditorGUI.indentLevel);
            }
        }

        private void DrawOneAction(IAction action, int indent)
        {
            using (new IndentLevelScope(indent))
            {
                if (action is CompositeAction compositeAction)
                {
                    _idFoldout.TryAdd(action.ID, false);
                    if (!compositeAction.IsDone)
                    {
                        using (new ColorScope(Color.green))
                        {
                            _idFoldout[action.ID] = EditorGUILayout.Foldout(_idFoldout[action.ID], $"- {compositeAction.DebugName}");
                        }
                    }
                    else
                    {
                        _idFoldout[action.ID] = EditorGUILayout.Foldout(_idFoldout[action.ID], $"{compositeAction.DebugName}");
                    }

                    if (_idFoldout[action.ID])
                    {
                        foreach (var subAc in compositeAction.GetSubActions())
                        {
                            DrawOneAction(subAc, indent + 1);
                        }
                    }
                }
                else if (action is BaseAction baseAction)
                {
                    if (!baseAction.IsDone)
                    {
                        using (new ColorScope(Color.green))
                        {
                            EditorGUILayout.LabelField($"- {baseAction.DebugName}");
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField(baseAction.DebugName);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField($"Action {action.ID}");
                }
            }
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
            if (visitorInfo.BundleVisitors.Length == 0)
            {
                EditorGUILayout.LabelField("Empty");
                return;
            }

            foreach (var bundleInfo in visitorInfo.BundleVisitors)
            {
                EditorGUILayout.LabelField(bundleInfo.BundlePath, $"{LiteEditorUtils.GetSizeString(bundleInfo.MemorySize)} {bundleInfo.RefCount} {bundleInfo.Stage}({bundleInfo.RetainTime:0.0}s)");

                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var assetInfo in bundleInfo.AssetVisitors)
                    {
                        EditorGUILayout.LabelField(assetInfo.AssetPath, $"{LiteEditorUtils.GetSizeString(assetInfo.MemorySize)} {assetInfo.RefCount} {assetInfo.Stage}({assetInfo.RetainTime:0.0}s)");
                    }
                }
            }
        }

        private void DrawTask()
        {
            var taskList = LiteRuntime.Task?.GetTaskList();
            if (taskList == null)
            {
                EditorGUILayout.LabelField("Empty");
            }
            else
            {
                EditorGUILayout.LabelField($"Tasks:{LiteRuntime.Task.RunningTaskCount}/{LiteRuntime.Task.PendingTaskCount}");
                taskList.Foreach(DrawOneTask, EditorGUI.indentLevel);
            }
        }

        private void DrawOneTask(ITask task, int indent)
        {
            using (new IndentLevelScope(indent))
            {
                var taskName = string.Empty;
                if (task is BaseObject baseObj)
                {
                    taskName = baseObj.DebugName;
                }
                else
                {
                    taskName = task.GetType().Name;
                }
                EditorGUILayout.LabelField($"{taskName} {task.Progress*100:0.0}% {task.State}");
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