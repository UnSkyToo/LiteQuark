using System;
using System.Collections.Generic;
using System.IO;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal class AssetViewerTreeView : TreeView
    {
        public event Action<AssetViewerTreeItem> OnItemSelectionChanged;

        private readonly Dictionary<int, AssetViewerTreeItem> ItemMap_ = new Dictionary<int, AssetViewerTreeItem>();
        private int SizeSortedAscending_ = 0;

        public AssetViewerTreeView(TreeViewState state)
            : base(state, CreateHeader())
        {
            multiColumnHeader.sortingChanged += OnHeaderSortingChanged;
            Reload();
        }

        private static MultiColumnHeader CreateHeader()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    width = 200,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type"),
                    width = 50,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Estimated Size"),
                    width = 150,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Path"),
                    width = 400,
                    canSort = false,
                },
            };

            var state = new MultiColumnHeaderState(columns);
            return new MultiColumnHeader(state);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };

            var items = new List<TreeViewItem>();
            ItemMap_.Clear();

            var collector = new ResCollector();
            var idCounter = 1;
            var packInfo = collector.GenerateBundlePackInfo(EditorUserBuildSettings.activeBuildTarget);
            foreach (var bundle in packInfo.BundleList)
            {
                var bundleFullPath = PathUtils.GetFullPathInAssetRoot(bundle.BundlePath).ToLower();
                var bundleItem = new AssetViewerTreeItem
                {
                    id = idCounter++,
                    displayName = $"{Path.GetFileNameWithoutExtension(bundle.BundlePath)}({bundle.AssetList.Length})",
                    depth = 0,
                    TypeIcon = null,
                    Type = "bundle",
                    Size = 0,
                    Path = bundleFullPath,
                    DependencyList = bundle.DependencyList,
                };

                var totalSize = 0L;
                foreach (var asset in bundle.AssetList)
                {
                    var assetFullPath = PathUtils.GetFullPathInAssetRoot(asset).ToLower();
                    var info = new FileInfo(assetFullPath);
                    if (!info.Exists)
                    {
                        Debug.LogError($"asset not exist : {assetFullPath}");
                        continue;
                    }

                    var size = info.Length;
                    totalSize += size;

                    var assetItem = new AssetViewerTreeItem
                    {
                        id = idCounter++,
                        displayName = Path.GetFileNameWithoutExtension(asset),
                        depth = 1,
                        TypeIcon = AssetDatabase.GetCachedIcon(assetFullPath),
                        Type = Path.GetExtension(asset).TrimStart('.'),
                        Size = size,
                        Path = assetFullPath,
                        DependencyList = Array.Empty<string>(),
                    };
                    bundleItem.AddChild(assetItem);
                    ItemMap_.Add(assetItem.id, assetItem);
                }

                AssetViewerUtils.SortTreeItemList(bundleItem.children, SizeSortedAscending_);

                bundleItem.Size = totalSize;

                items.Add(bundleItem);
                ItemMap_.Add(bundleItem.id, bundleItem);
            }

            AssetViewerUtils.SortTreeItemList(items, SizeSortedAscending_);

            SetupParentsAndChildrenFromDepths(root, items);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is not AssetViewerTreeItem item)
            {
                return;
            }

            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var rect = args.GetCellRect(i);
                var columnIndex = args.GetColumn(i);

                switch (columnIndex)
                {
                    case 0:
                        rect.xMin += GetContentIndent(item);
                        if (item.depth == 0)
                        {
                            EditorGUI.LabelField(rect, item.displayName);
                        }
                        else
                        {
                            EditorGUI.LabelField(rect, new GUIContent
                            {
                                text = item.displayName,
                                image = item.TypeIcon,
                            });
                        }

                        break;
                    case 1:
                        EditorGUI.LabelField(rect, item.Type);
                        break;
                    case 2:
                        EditorGUI.LabelField(rect, AssetViewerUtils.GetSizeString(item.Size));
                        break;
                    case 3:
                        EditorGUI.LabelField(rect, item.Path);
                        break;
                }
            }
        }

        private void OnHeaderSortingChanged(MultiColumnHeader header)
        {
            switch (header.sortedColumnIndex)
            {
                case 2: // size
                    SizeSortedAscending_ = header.state.columns[2].sortedAscending ? 1 : -1;
                    Reload();
                    break;
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            if (selectedIds.Count == 1 && ItemMap_.TryGetValue(selectedIds[0], out var item))
            {
                OnItemSelectionChanged?.Invoke(item);
                
                if (Event.current.alt)
                {
                    if (item.depth == 0)
                    {
                        var folderPath = item.Path.Substring(0, item.Path.Length - 3);
                        LiteEditorUtils.Ping(folderPath);
                    }
                    else
                    {
                        LiteEditorUtils.Ping(item.Path);
                    }
                }
            }
        }
    }
}