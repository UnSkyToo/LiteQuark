using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LiteQuark.Editor.BundleViewer
{
    internal class BundleViewerTreeView : TreeView
    {
        public event Action<BundleViewerTreeItem> OnItemSelectionChanged;

        public bool CombineMode { get; set; } = true;

        public bool IsLoaded { get; private set; } = false;

        private const string BundleType = "bundle";

        private readonly Dictionary<int, BundleViewerTreeItem> _itemCacheMap = new Dictionary<int, BundleViewerTreeItem>();
        private readonly Dictionary<string, BundleViewerTreeItem> _bundlePathCacheMap = new Dictionary<string, BundleViewerTreeItem>();
        private int _idGenerator = 1;
        private int _sizeSortedAscendingType = 0;
        
        public BundleViewerTreeView(TreeViewState state)
            : base(state, CreateHeader())
        {
            multiColumnHeader.sortingChanged += OnHeaderSortingChanged;
            EditorApplication.delayCall += Reload;
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
            _idGenerator = 1;
            
            var collector = new ResCollector();
            var packInfo = collector.GetVersionPackInfo(PlayerSettings.bundleVersion, EditorUserBuildSettings.activeBuildTarget, false, false);
            
            var items = CombineMode ? BuildWithCombineMode(packInfo) : BuildWithNormalMode(packInfo);
            BundleViewerUtils.SortTreeItemList(items, _sizeSortedAscendingType);
            
            SetupParentsAndChildrenFromDepths(root, items);
            
            IsLoaded = true;
            return root;
        }

        private List<TreeViewItem> BuildWithNormalMode(VersionPackInfo packInfo)
        {
            var items = new List<TreeViewItem>();
            _itemCacheMap.Clear();
            _bundlePathCacheMap.Clear();
            
            foreach (var bundle in packInfo.BundleList)
            {
                var bundleItem = CreateBundleItem(bundle.BundlePath, $"{Path.GetFileNameWithoutExtension(bundle.BundlePath)}({bundle.AssetList.Length})", 0);
                items.Add(bundleItem);
                _itemCacheMap.Add(bundleItem.id, bundleItem);
                _bundlePathCacheMap.Add(bundle.BundlePath, bundleItem);

                var totalSize = 0L;
                foreach (var assetPath in bundle.AssetList)
                {
                    var assetItem = CreateAssetItem(assetPath, bundleItem.depth + 1);
                    totalSize += assetItem.Size;
                    bundleItem.AddChild(assetItem);
                    _itemCacheMap.Add(assetItem.id, assetItem);
                }
                bundleItem.Size = totalSize;
                bundleItem.DependencyList = bundle.DependencyList;

                BundleViewerUtils.SortTreeItemList(bundleItem.children, _sizeSortedAscendingType);
            }

            return items;
        }

        private List<TreeViewItem> BuildWithCombineMode(VersionPackInfo packInfo)
        {
            var items = new List<TreeViewItem>();
            _itemCacheMap.Clear();
            _bundlePathCacheMap.Clear();

            var treeDict = new Dictionary<string, BundleViewerTreeItem>();

            foreach (var bundle in packInfo.BundleList)
            {
                var bundleItem = default(BundleViewerTreeItem);
                var subPaths = bundle.BundlePath.Split(PathUtils.DirectorySeparatorChar);
                var stepPath = new StringBuilder();
                
                for (var i = 0; i < subPaths.Length; ++i)
                {
                    stepPath.Append(subPaths[i]);
                    stepPath.Append(PathUtils.DirectorySeparatorChar);
                    var key = stepPath.ToString().TrimEnd(PathUtils.DirectorySeparatorChar);
                    
                    if (!treeDict.TryGetValue(key, out var item))
                    {
                        item = CreateBundleItem(key, subPaths[i], i);
                        treeDict.Add(key, item);

                        if (bundleItem == null)
                        {
                            items.Add(item);
                        }
                        else
                        {
                            bundleItem.AddChild(item);
                        }
                        _itemCacheMap.Add(item.id, item);
                    }
                    
                    bundleItem = item;
                }
                
                _bundlePathCacheMap.Add(bundle.BundlePath, bundleItem);
                
                var totalSize = 0L;
                foreach (var assetPath in bundle.AssetList)
                {
                    var assetItem = CreateAssetItem(assetPath, bundleItem.depth + 1);
                    totalSize += assetItem.Size;
                    bundleItem.AddChild(assetItem);
                    _itemCacheMap.Add(assetItem.id, assetItem);
                }
                bundleItem.Size = totalSize;
                bundleItem.DependencyList = bundle.DependencyList;

                BundleViewerUtils.SortTreeItemList(bundleItem.children, _sizeSortedAscendingType);

                var cacheItem = bundleItem.parent as BundleViewerTreeItem;
                while (cacheItem != null)
                {
                    cacheItem.Size += totalSize;
                    cacheItem = cacheItem.parent as BundleViewerTreeItem;
                }
            }

            return items;
        }

        private BundleViewerTreeItem CreateBundleItem(string bundlePath, string displayName, int depth)
        {
            var bundleFullPath = PathUtils.GetFullPathInAssetRoot(bundlePath).ToLower();
            
            var bundleItem = new BundleViewerTreeItem
            {
                id = _idGenerator++,
                displayName = displayName,
                depth = depth,
                TypeIcon = null,
                Type = BundleType,
                Size = 0,
                Path = bundleFullPath,
                DependencyList = Array.Empty<string>(),
            };

            return bundleItem;
        }

        private BundleViewerTreeItem CreateAssetItem(string assetPath, int depth)
        {
            var assetFullPath = PathUtils.GetFullPathInAssetRoot(assetPath).ToLower();
            var info = new FileInfo(assetFullPath);
            if (!info.Exists)
            {
                Debug.LogError($"asset not exist : {assetFullPath}");
                return null;
            }
            
            var assetItem = new BundleViewerTreeItem
            {
                id = _idGenerator++,
                displayName = Path.GetFileNameWithoutExtension(assetPath),
                depth = depth,
                TypeIcon = AssetDatabase.GetCachedIcon(assetFullPath),
                Type = Path.GetExtension(assetPath).TrimStart('.'),
                Size = info.Length,
                Path = assetFullPath,
                DependencyList = Array.Empty<string>(),
            };

            return assetItem;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item is not BundleViewerTreeItem item)
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
                        EditorGUI.LabelField(rect, LiteEditorUtils.GetSizeString(item.Size));
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
                    _sizeSortedAscendingType = header.state.columns[2].sortedAscending ? 1 : -1;
                    Reload();
                    break;
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            if (selectedIds.Count == 1 && _itemCacheMap.TryGetValue(selectedIds[0], out var item))
            {
                OnItemSelectionChanged?.Invoke(item);
                
                if (Event.current.alt)
                {
                    if (item.Type == BundleType)
                    {
                        var folderPath = item.Path.Replace(LiteConst.BundleFileExt, string.Empty);
                        LiteEditorUtils.Ping(folderPath);
                    }
                    else
                    {
                        LiteEditorUtils.Ping(item.Path);
                    }
                }
            }
        }

        public BundleViewerTreeItem GetDependencyTreeItem(string path)
        {
            return _bundlePathCacheMap.GetValueOrDefault(path);
        }
    }
}