using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using LiteQuark.Runtime;

namespace LiteQuark.Editor.LoadProfiler
{
    internal class ProfilerTreeView : UnityEditor.IMGUI.Controls.TreeView
    {
        public event Action<ProfilerTreeItem> OnItemSelectionChanged;
        public event Action<List<ProfilerTreeItem>> OnMultipleItemsSelected;

        public enum ViewMode
        {
            Sessions,
            Records,
            Hotspots,
            Timeline
        }

        public ViewMode CurrentViewMode { get; set; } = ViewMode.Sessions;

        private readonly Dictionary<int, ProfilerTreeItem> _itemCacheMap = new Dictionary<int, ProfilerTreeItem>();
        private int _idGenerator = 1;
        private int _durationSortedAscendingType; // 0=default, 1=ascending, -1=descending

        public ProfilerTreeView(TreeViewState state)
            : base(state, CreateHeader())
        {
            multiColumnHeader.sortingChanged += OnHeaderSortingChanged;
            showAlternatingRowBackgrounds = true;
            rowHeight = 20;
        }

        private static MultiColumnHeader CreateHeader()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    width = 250,
                    minWidth = 100,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type"),
                    width = 60,
                    minWidth = 40,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Source"),
                    width = 60,
                    minWidth = 40,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Duration (ms)"),
                    width = 100,
                    minWidth = 60,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Size"),
                    width = 80,
                    minWidth = 50,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Status"),
                    width = 60,
                    minWidth = 40,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Path"),
                    width = 300,
                    minWidth = 100,
                    canSort = false,
                },
            };

            var state = new MultiColumnHeaderState(columns);
            return new MultiColumnHeader(state);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            _idGenerator = 1;
            _itemCacheMap.Clear();

            var items = new List<TreeViewItem>();

            if (!Application.isPlaying)
            {
                SetupParentsAndChildrenFromDepths(root, items);
                return root;
            }

            switch (CurrentViewMode)
            {
                case ViewMode.Sessions:
                    items = BuildSessionView();
                    break;
                case ViewMode.Records:
                    items = BuildRecordView();
                    break;
                case ViewMode.Hotspots:
                    items = BuildHotspotView();
                    break;
            }

            SetupParentsAndChildrenFromDepths(root, items);
            return root;
        }

        private List<TreeViewItem> BuildSessionView()
        {
            var items = new List<TreeViewItem>();
            var sessions = ProfilerCore.Instance.GetRecentSessions(100);

            // 根据排序设置对 Sessions 排序
            if (_durationSortedAscendingType != 0)
            {
                sessions.Sort((a, b) => _durationSortedAscendingType * a.TotalDuration.CompareTo(b.TotalDuration));
            }
            else
            {
                // 默认按时间倒序（最新的在前）
                sessions.Reverse();
            }

            foreach (var session in sessions)
            {
                var sessionItem = new ProfilerTreeItem
                {
                    id = _idGenerator++,
                    displayName = $"[{session.SessionId}] {PathUtils.GetFileName(session.RequestPath)}",
                    depth = 0,
                    Path = session.RequestPath,
                    Duration = session.TotalDuration,
                    Size = session.TotalSize,
                    IsSuccess = session.IsSuccess,
                    SessionId = session.SessionId,
                    LoadType = AssetLoadEventType.Session,
                    LoadSource = AssetLoadEventSource.Unknown,
                };
                items.Add(sessionItem);
                _itemCacheMap[sessionItem.id] = sessionItem;

                // Use dependency tree if available, otherwise fall back to flat records list
                if (session.DependencyTree != null && session.DependencyTree.Children.Count > 0)
                {
                    AddDependencyNodeChildren(sessionItem, session.DependencyTree, session.Records);
                }
                else
                {
                    // Fallback: Add records as flat children (sorted by duration if needed)
                    var recordsToAdd = new List<ProfilerRecord>(session.Records);
                    if (_durationSortedAscendingType != 0)
                    {
                        recordsToAdd.Sort((a, b) => _durationSortedAscendingType * a.Duration.CompareTo(b.Duration));
                    }
                    foreach (var record in recordsToAdd)
                    {
                        var recordItem = CreateRecordItem(record, 1);
                        sessionItem.AddChild(recordItem);
                        _itemCacheMap[recordItem.id] = recordItem;
                    }
                }
            }

            return items;
        }

        private void AddDependencyNodeChildren(ProfilerTreeItem parent, ProfilerDependencyNode node, List<ProfilerRecord> records)
        {
            // 对子节点按 Duration 排序（如果需要）
            var children = new List<ProfilerDependencyNode>(node.Children);
            if (_durationSortedAscendingType != 0)
            {
                children.Sort((a, b) => _durationSortedAscendingType * a.Duration.CompareTo(b.Duration));
            }

            foreach (var child in children)
            {
                // DependencyNode 现在直接持有 Record 引用
                var record = child.Record;

                var childItem = new ProfilerTreeItem
                {
                    id = _idGenerator++,
                    displayName = PathUtils.GetFileName(child.Path),
                    depth = parent.depth + 1,
                    Path = child.Path,
                    Duration = child.Duration,
                    Size = child.Size,
                    IsSuccess = child.IsSuccess,
                    RecordId = child.RecordId,
                    SessionId = parent.SessionId,
                    LoadType = child.Type,
                    LoadSource = child.Source,
                    Dependencies = record?.Dependencies,
                    Timestamp = record?.StartTimestamp ?? 0,
                };

                parent.AddChild(childItem);
                _itemCacheMap[childItem.id] = childItem;

                // Recursively add children
                if (child.Children.Count > 0)
                {
                    AddDependencyNodeChildren(childItem, child, records);
                }
            }
        }

        private List<TreeViewItem> BuildRecordView()
        {
            var items = new List<TreeViewItem>();
            var records = ProfilerCore.Instance.GetRecentRecords(500);

            // Apply sorting if duration column is sorted
            if (_durationSortedAscendingType != 0)
            {
                records.Sort((a, b) => _durationSortedAscendingType * a.Duration.CompareTo(b.Duration));
            }
            else
            {
                // Default: newest first
                for (var i = records.Count - 1; i >= 0; i--)
                {
                    var recordItem = CreateRecordItem(records[i], 0);
                    items.Add(recordItem);
                    _itemCacheMap[recordItem.id] = recordItem;
                }
                return items;
            }

            foreach (var record in records)
            {
                var recordItem = CreateRecordItem(record, 0);
                items.Add(recordItem);
                _itemCacheMap[recordItem.id] = recordItem;
            }

            return items;
        }

        private List<TreeViewItem> BuildHotspotView()
        {
            var items = new List<TreeViewItem>();
            var records = ProfilerCore.Instance.GetHotspotRecords(50);

            foreach (var record in records)
            {
                var recordItem = CreateRecordItem(record, 0);
                items.Add(recordItem);
                _itemCacheMap[recordItem.id] = recordItem;
            }

            return items;
        }

        private ProfilerTreeItem CreateRecordItem(ProfilerRecord record, int depth)
        {
            var displayName = record.Type == AssetLoadEventType.Bundle
                ? PathUtils.GetFileName(record.BundlePath)
                : PathUtils.GetFileName(record.AssetPath);

            return new ProfilerTreeItem
            {
                id = _idGenerator++,
                displayName = displayName,
                depth = depth,
                Path = record.Type == AssetLoadEventType.Bundle ? record.BundlePath : record.AssetPath,
                Duration = record.Duration,
                Size = record.Size,
                IsSuccess = record.IsSuccess,
                RecordId = record.RecordId,
                SessionId = record.SessionId,
                LoadType = record.Type,
                LoadSource = record.Source,
                Dependencies = record.Dependencies,
                Timestamp = record.StartTimestamp,
            };
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is not ProfilerTreeItem item)
            {
                return;
            }

            for (var i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                var rect = args.GetCellRect(i);
                var columnIndex = args.GetColumn(i);

                switch (columnIndex)
                {
                    case 0: // Name
                        rect.xMin += GetContentIndent(item);
                        var color = item.IsSuccess ? ProfilerStyles.GetLoadTypeColor(item.LoadType) : ProfilerStyles.ErrorColor;
                        using (new ColorScope(color))
                        {
                            EditorGUI.LabelField(rect, item.displayName);
                        }
                        break;
                    case 1: // Type
                        EditorGUI.LabelField(rect, item.LoadType.ToString());
                        break;
                    case 2: // Source
                        using (new ColorScope(ProfilerStyles.GetLoadSourceColor(item.LoadSource)))
                        {
                            EditorGUI.LabelField(rect, item.LoadSource.ToString());
                        }
                        break;
                    case 3: // Duration
                        var durationColor = GetDurationColor(item.Duration);
                        using (new ColorScope(durationColor))
                        {
                            EditorGUI.LabelField(rect, $"{item.Duration:F1}");
                        }
                        break;
                    case 4: // Size
                        EditorGUI.LabelField(rect, item.Size > 0 ? LiteEditorUtils.GetSizeString(item.Size) : "-");
                        break;
                    case 5: // Status
                        using (new ColorScope(item.IsSuccess ? Color.green : ProfilerStyles.ErrorColor))
                        {
                            EditorGUI.LabelField(rect, item.IsSuccess ? "OK" : "FAIL");
                        }
                        break;
                    case 6: // Path
                        EditorGUI.LabelField(rect, item.Path);
                        break;
                }
            }
        }

        private Color GetDurationColor(float duration)
        {
            if (duration < 10) return Color.green;
            if (duration < 50) return Color.yellow;
            if (duration < 100) return new Color(1f, 0.6f, 0f);
            return ProfilerStyles.ErrorColor;
        }

        private void OnHeaderSortingChanged(MultiColumnHeader header)
        {
            // Hotspots 模式不支持排序（已经是按耗时降序）
            if (CurrentViewMode == ViewMode.Hotspots)
            {
                return;
            }

            if (header.sortedColumnIndex == 3) // Duration column
            {
                _durationSortedAscendingType = header.IsSortedAscending(3) ? 1 : -1;
            }
            else
            {
                // 其他列点击时清除排序状态
                _durationSortedAscendingType = 0;
            }
            Reload();
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            if (selectedIds.Count == 1 && _itemCacheMap.TryGetValue(selectedIds[0], out var item))
            {
                OnItemSelectionChanged?.Invoke(item);
            }
            else if (selectedIds.Count > 1)
            {
                // 多选支持
                var selectedItems = new List<ProfilerTreeItem>();
                foreach (var id in selectedIds)
                {
                    if (_itemCacheMap.TryGetValue(id, out var selectedItem))
                    {
                        selectedItems.Add(selectedItem);
                    }
                }
                if (selectedItems.Count > 0)
                {
                    OnMultipleItemsSelected?.Invoke(selectedItems);
                    // 同时通知单选事件（使用第一个选中项）
                    OnItemSelectionChanged?.Invoke(selectedItems[0]);
                }
            }
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return true;
            }

            if (item is ProfilerTreeItem profilerItem)
            {
                var lowerSearch = search.ToLowerInvariant();
                return profilerItem.displayName.ToLowerInvariant().Contains(lowerSearch) ||
                       profilerItem.Path.ToLowerInvariant().Contains(lowerSearch);
            }

            return base.DoesItemMatchSearch(item, search);
        }

        /// <summary>
        /// 通过 RecordId 选中对应的项目
        /// </summary>
        public bool SelectByRecordId(long recordId)
        {
            foreach (var kvp in _itemCacheMap)
            {
                if (kvp.Value.RecordId == recordId)
                {
                    SetSelection(new List<int> { kvp.Key }, TreeViewSelectionOptions.RevealAndFrame);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 通过 SessionId 选中对应的项目
        /// </summary>
        public bool SelectBySessionId(long sessionId)
        {
            foreach (var kvp in _itemCacheMap)
            {
                if (kvp.Value.SessionId == sessionId && kvp.Value.LoadType == AssetLoadEventType.Session)
                {
                    SetSelection(new List<int> { kvp.Key }, TreeViewSelectionOptions.RevealAndFrame);
                    return true;
                }
            }
            return false;
        }
    }
}
