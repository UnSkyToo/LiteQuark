using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LiteQuark.Runtime;

namespace LiteQuark.Editor
{
    public class VersionViewerWindow : EditorWindow
    {
        [MenuItem("Lite/Asset/Version Viewer")]
        public static void ShowWindow()
        {
            var win = GetWindow<VersionViewerWindow>("Version Viewer");
            win.minSize = new Vector2(900, 600);
            win.Show();
        }

        // 文件选择
        private string _oldVersionPath = string.Empty;
        private string _newVersionPath = string.Empty;
        private VersionPackInfo _oldVersionInfo;
        private VersionPackInfo _newVersionInfo;

        // 差异数据
        private List<BundleDiffInfo> _diffList = new();
        private bool _hasDiff = false;

        // UI 状态
        private Vector2 _scrollPosition;
        private DiffFilter _currentFilter = DiffFilter.All;
        private string _searchText = string.Empty;
        private bool _showDetails = true;
        
        // 排序
        private SortColumn _sortColumn = SortColumn.Status;
        private bool _sortAscending = true;

        // 统计信息
        private int _addedCount;
        private int _removedCount;
        private int _modifiedCount;
        private int _unchangedCount;
        private long _totalSizeChange;

        private enum DiffFilter
        {
            All,
            Added,
            Removed,
            Modified,
            Unchanged
        }

        private enum SortColumn
        {
            Status,
            BundlePath,
            SizeChange
        }

        private enum DiffStatus
        {
            Added,      // 新增
            Removed,    // 删除
            Modified,   // 修改
            Unchanged   // 未变化
        }

        private class BundleDiffInfo
        {
            public DiffStatus Status;
            public string BundlePath;
            public BundleInfo OldBundle;
            public BundleInfo NewBundle;
            public long SizeChange;
            public bool HashChanged;
            public List<string> AddedAssets = new();
            public List<string> RemovedAssets = new();
            public List<string> AddedDependencies = new();
            public List<string> RemovedDependencies = new();
            public bool IsExpanded;
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawFileSelection();
            
            if (_hasDiff)
            {
                DrawStatistics();
                DrawFilterBar();
                DrawDiffList();
            }
            else
            {
                DrawEmptyState();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ClearAll();
            }

            if (GUILayout.Button("Decrypt", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                DecryptVersionFile();
            }
            
            GUILayout.FlexibleSpace();
            
            if (_hasDiff)
            {
                if (GUILayout.Button("Export Report", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    ExportReport();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFileSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Version Files", EditorStyles.boldLabel);
            
            // 旧版本
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Old Version:", GUILayout.Width(80));
            
            GUI.enabled = false;
            EditorGUILayout.TextField(_oldVersionPath);
            GUI.enabled = true;
            
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                SelectVersionFile(true);
            }
            EditorGUILayout.EndHorizontal();
            
            if (_oldVersionInfo is { IsValid: true })
            {
                EditorGUILayout.LabelField($"   Version: {_oldVersionInfo.Version} | Platform: {_oldVersionInfo.Platform} | Bundles: {_oldVersionInfo.BundleList.Length}", EditorStyles.miniLabel);
            }

            // 新版本
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("New Version:", GUILayout.Width(80));
            
            GUI.enabled = false;
            EditorGUILayout.TextField(_newVersionPath);
            GUI.enabled = true;
            
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                SelectVersionFile(false);
            }
            EditorGUILayout.EndHorizontal();
            
            if (_newVersionInfo is { IsValid: true })
            {
                EditorGUILayout.LabelField($"   Version: {_newVersionInfo.Version} | Platform: {_newVersionInfo.Platform} | Bundles: {_newVersionInfo.BundleList.Length}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(5);
            
            // 对比按钮
            GUI.enabled = !string.IsNullOrEmpty(_oldVersionPath) && !string.IsNullOrEmpty(_newVersionPath);
            if (GUILayout.Button("Compare Versions", GUILayout.Height(30)))
            {
                CompareVersions();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndVertical();
        }

        private void DrawStatistics()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            DrawStatBox("Added", _addedCount, new Color(0.2f, 0.8f, 0.2f));
            DrawStatBox("Removed", _removedCount, new Color(0.9f, 0.3f, 0.3f));
            DrawStatBox("Modified", _modifiedCount, new Color(0.9f, 0.7f, 0.2f));
            DrawStatBox("Unchanged", _unchangedCount, Color.gray);
            
            GUILayout.FlexibleSpace();
            
            // 总大小变化
            var sizeChangeText = _totalSizeChange >= 0 ? $"+{FormatSize(_totalSizeChange)}" : FormatSize(_totalSizeChange);
            var sizeColor = _totalSizeChange >= 0 ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.2f, 0.8f, 0.2f);
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Size Change", EditorStyles.miniLabel);
            var oldColor = GUI.color;
            GUI.color = sizeColor;
            EditorGUILayout.LabelField(sizeChangeText, EditorStyles.boldLabel);
            GUI.color = oldColor;
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatBox(string label, int count, Color color)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(80));
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            var oldColor = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField(count.ToString(), EditorStyles.boldLabel);
            GUI.color = oldColor;
            EditorGUILayout.EndVertical();
        }

        private void DrawFilterBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // 筛选按钮
            if (DrawFilterButton("All", DiffFilter.All)) _currentFilter = DiffFilter.All;
            if (DrawFilterButton($"Added ({_addedCount})", DiffFilter.Added)) _currentFilter = DiffFilter.Added;
            if (DrawFilterButton($"Removed ({_removedCount})", DiffFilter.Removed)) _currentFilter = DiffFilter.Removed;
            if (DrawFilterButton($"Modified ({_modifiedCount})", DiffFilter.Modified)) _currentFilter = DiffFilter.Modified;
            if (DrawFilterButton($"Unchanged ({_unchangedCount})", DiffFilter.Unchanged)) _currentFilter = DiffFilter.Unchanged;
            
            GUILayout.FlexibleSpace();
            
            // 搜索框
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField, GUILayout.Width(200));
            
            // 显示详情开关
            _showDetails = GUILayout.Toggle(_showDetails, "Details", EditorStyles.toolbarButton, GUILayout.Width(60));
            
            EditorGUILayout.EndHorizontal();
        }

        private bool DrawFilterButton(string text, DiffFilter filter)
        {
            var isSelected = _currentFilter == filter;
            var style = isSelected ? GetSelectedButtonStyle() : EditorStyles.toolbarButton;
            return GUILayout.Button(text, style, GUILayout.Width(100));
        }

        private GUIStyle GetSelectedButtonStyle()
        {
            var style = new GUIStyle(EditorStyles.toolbarButton);
            style.normal.textColor = Color.white;
            style.normal.background = MakeTex(1, 1, new Color(0.2f, 0.4f, 0.8f));
            return style;
        }

        private void DrawDiffList()
        {
            // 表头
            DrawListHeader();
            
            // 列表内容
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            var filteredList = GetFilteredList();
            
            foreach (var diff in filteredList)
            {
                DrawDiffItem(diff);
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawListHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Status", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ToggleSort(SortColumn.Status);
            }
            
            if (GUILayout.Button("Bundle Path", EditorStyles.toolbarButton))
            {
                ToggleSort(SortColumn.BundlePath);
            }
            
            if (GUILayout.Button("Size Change", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ToggleSort(SortColumn.SizeChange);
            }
            
            GUILayout.Space(20); // 为展开按钮留空间
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDiffItem(BundleDiffInfo diff)
        {
            var bgColor = GetStatusBackgroundColor(diff.Status);
            var oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = oldBgColor;
            
            EditorGUILayout.BeginHorizontal();
            
            // 状态标签
            var statusColor = GetStatusColor(diff.Status);
            var statusText = GetStatusText(diff.Status);
            DrawColorLabel(statusText, statusColor, 70);
            
            // Bundle 路径
            EditorGUILayout.LabelField(diff.BundlePath, EditorStyles.label);
            
            // 大小变化
            if (diff.Status != DiffStatus.Removed && diff.Status != DiffStatus.Added)
            {
                var sizeText = diff.SizeChange >= 0 ? $"+{FormatSize(diff.SizeChange)}" : FormatSize(diff.SizeChange);
                var sizeColor = diff.SizeChange >= 0 ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.2f, 0.8f, 0.2f);
                if (diff.SizeChange == 0) sizeColor = Color.gray;
                DrawColorLabel(sizeText, sizeColor, 90);
            }
            else
            {
                var size = diff.Status == DiffStatus.Added ? diff.NewBundle?.Size ?? 0 : diff.OldBundle?.Size ?? 0;
                var prefix = diff.Status == DiffStatus.Added ? "+" : "-";
                DrawColorLabel($"{prefix}{FormatSize(size)}", GetStatusColor(diff.Status), 90);
            }
            
            // 展开/折叠按钮
            if (_showDetails && HasDetails(diff))
            {
                if (GUILayout.Button(diff.IsExpanded ? "▼" : "▶", GUILayout.Width(20)))
                {
                    diff.IsExpanded = !diff.IsExpanded;
                }
            }
            else
            {
                GUILayout.Space(24);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 详情展开
            if (_showDetails && diff.IsExpanded && HasDetails(diff))
            {
                DrawDiffDetails(diff);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawDiffDetails(BundleDiffInfo diff)
        {
            EditorGUI.indentLevel++;
            
            // Hash 变化
            if (diff.HashChanged && diff.OldBundle != null && diff.NewBundle != null)
            {
                EditorGUILayout.LabelField($"Hash: {diff.OldBundle.Hash} → {diff.NewBundle.Hash}", EditorStyles.miniLabel);
            }
            
            // 新增资源
            if (diff.AddedAssets.Count > 0)
            {
                EditorGUILayout.LabelField($"Added Assets ({diff.AddedAssets.Count}):", EditorStyles.miniBoldLabel);
                foreach (var asset in diff.AddedAssets.Take(10))
                {
                    DrawColorLabel($"  + {asset}", new Color(0.2f, 0.8f, 0.2f), 0);
                }
                if (diff.AddedAssets.Count > 10)
                {
                    EditorGUILayout.LabelField($"  ... and {diff.AddedAssets.Count - 10} more", EditorStyles.miniLabel);
                }
            }
            
            // 删除资源
            if (diff.RemovedAssets.Count > 0)
            {
                EditorGUILayout.LabelField($"Removed Assets ({diff.RemovedAssets.Count}):", EditorStyles.miniBoldLabel);
                foreach (var asset in diff.RemovedAssets.Take(10))
                {
                    DrawColorLabel($"  - {asset}", new Color(0.9f, 0.3f, 0.3f), 0);
                }
                if (diff.RemovedAssets.Count > 10)
                {
                    EditorGUILayout.LabelField($"  ... and {diff.RemovedAssets.Count - 10} more", EditorStyles.miniLabel);
                }
            }
            
            // 依赖变化
            if (diff.AddedDependencies.Count > 0)
            {
                EditorGUILayout.LabelField($"Added Dependencies ({diff.AddedDependencies.Count}):", EditorStyles.miniBoldLabel);
                foreach (var dep in diff.AddedDependencies)
                {
                    DrawColorLabel($"  + {dep}", new Color(0.2f, 0.8f, 0.2f), 0);
                }
            }
            
            if (diff.RemovedDependencies.Count > 0)
            {
                EditorGUILayout.LabelField($"Removed Dependencies ({diff.RemovedDependencies.Count}):", EditorStyles.miniBoldLabel);
                foreach (var dep in diff.RemovedDependencies)
                {
                    DrawColorLabel($"  - {dep}", new Color(0.9f, 0.3f, 0.3f), 0);
                }
            }
            
            EditorGUI.indentLevel--;
        }

        private void DrawColorLabel(string text, Color color, int width)
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = color;
            
            if (width > 0)
            {
                EditorGUILayout.LabelField(text, style, GUILayout.Width(width));
            }
            else
            {
                EditorGUILayout.LabelField(text, style);
            }
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Select version files", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void SelectVersionFile(bool isOld)
        {
            var path = EditorUtility.OpenFilePanelWithFilters("Open Version File", PathUtils.GetLiteQuarkRootPath(string.Empty), new string[] { "Version File", "txt" });
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            
            try
            {
                var data = System.IO.File.ReadAllBytes(path);
                var info = VersionPackInfo.FromBinaryData(data);
                info.Initialize();
                
                if (isOld)
                {
                    _oldVersionPath = path;
                    _oldVersionInfo = info;
                }
                else
                {
                    _newVersionPath = path;
                    _newVersionInfo = info;
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load version file:\n{ex.Message}", "OK");
            }
        }

        private void CompareVersions()
        {
            if (_oldVersionInfo == null || _newVersionInfo == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select both version files first.", "OK");
                return;
            }
            
            _diffList.Clear();
            
            var oldBundles = _oldVersionInfo.BundleList.ToDictionary(b => b.BundlePath);
            var newBundles = _newVersionInfo.BundleList.ToDictionary(b => b.BundlePath);
            
            var allPaths = oldBundles.Keys.Union(newBundles.Keys).ToList();
            
            foreach (var path in allPaths)
            {
                var hasOld = oldBundles.TryGetValue(path, out var oldBundle);
                var hasNew = newBundles.TryGetValue(path, out var newBundle);
                
                var diff = new BundleDiffInfo
                {
                    BundlePath = path,
                    OldBundle = oldBundle,
                    NewBundle = newBundle
                };
                
                if (!hasOld && hasNew)
                {
                    // 新增
                    diff.Status = DiffStatus.Added;
                    diff.AddedAssets = newBundle.AssetList.ToList();
                    diff.AddedDependencies = newBundle.DependencyList.ToList();
                    diff.SizeChange = newBundle.Size;
                }
                else if (hasOld && !hasNew)
                {
                    // 删除
                    diff.Status = DiffStatus.Removed;
                    diff.RemovedAssets = oldBundle.AssetList.ToList();
                    diff.RemovedDependencies = oldBundle.DependencyList.ToList();
                    diff.SizeChange = -oldBundle.Size;
                }
                else
                {
                    // 比较变化
                    diff.HashChanged = oldBundle.Hash != newBundle.Hash;
                    diff.SizeChange = newBundle.Size - oldBundle.Size;
                    
                    var oldAssets = new HashSet<string>(oldBundle.AssetList);
                    var newAssets = new HashSet<string>(newBundle.AssetList);
                    diff.AddedAssets = newAssets.Except(oldAssets).ToList();
                    diff.RemovedAssets = oldAssets.Except(newAssets).ToList();
                    
                    var oldDeps = new HashSet<string>(oldBundle.DependencyList);
                    var newDeps = new HashSet<string>(newBundle.DependencyList);
                    diff.AddedDependencies = newDeps.Except(oldDeps).ToList();
                    diff.RemovedDependencies = oldDeps.Except(newDeps).ToList();
                    
                    if (diff.HashChanged || diff.SizeChange != 0 || 
                        diff.AddedAssets.Count > 0 || diff.RemovedAssets.Count > 0 ||
                        diff.AddedDependencies.Count > 0 || diff.RemovedDependencies.Count > 0)
                    {
                        diff.Status = DiffStatus.Modified;
                    }
                    else
                    {
                        diff.Status = DiffStatus.Unchanged;
                    }
                }
                
                _diffList.Add(diff);
            }
            
            // 统计
            _addedCount = _diffList.Count(d => d.Status == DiffStatus.Added);
            _removedCount = _diffList.Count(d => d.Status == DiffStatus.Removed);
            _modifiedCount = _diffList.Count(d => d.Status == DiffStatus.Modified);
            _unchangedCount = _diffList.Count(d => d.Status == DiffStatus.Unchanged);
            
            _totalSizeChange = _diffList.Where(d => d.Status == DiffStatus.Added).Sum(d => d.NewBundle?.Size ?? 0)
                             - _diffList.Where(d => d.Status == DiffStatus.Removed).Sum(d => d.OldBundle?.Size ?? 0)
                             + _diffList.Where(d => d.Status == DiffStatus.Modified).Sum(d => d.SizeChange);
            
            _hasDiff = true;
            
            // 默认排序
            SortList();
        }

        private List<BundleDiffInfo> GetFilteredList()
        {
            var list = _diffList.AsEnumerable();
            
            // 状态筛选
            if (_currentFilter != DiffFilter.All)
            {
                var targetStatus = _currentFilter switch
                {
                    DiffFilter.Added => DiffStatus.Added,
                    DiffFilter.Removed => DiffStatus.Removed,
                    DiffFilter.Modified => DiffStatus.Modified,
                    DiffFilter.Unchanged => DiffStatus.Unchanged,
                    _ => DiffStatus.Unchanged
                };
                list = list.Where(d => d.Status == targetStatus);
            }
            
            // 搜索筛选
            if (!string.IsNullOrEmpty(_searchText))
            {
                list = list.Where(d => d.BundlePath.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            
            return list.ToList();
        }

        private void ToggleSort(SortColumn column)
        {
            if (_sortColumn == column)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = column;
                _sortAscending = true;
            }
            SortList();
        }

        private void SortList()
        {
            switch (_sortColumn)
            {
                case SortColumn.Status:
                    _diffList = _sortAscending 
                        ? _diffList.OrderBy(d => d.Status).ToList()
                        : _diffList.OrderByDescending(d => d.Status).ToList();
                    break;
                case SortColumn.BundlePath:
                    _diffList = _sortAscending 
                        ? _diffList.OrderBy(d => d.BundlePath).ToList()
                        : _diffList.OrderByDescending(d => d.BundlePath).ToList();
                    break;
                case SortColumn.SizeChange:
                    _diffList = _sortAscending 
                        ? _diffList.OrderBy(d => d.SizeChange).ToList()
                        : _diffList.OrderByDescending(d => d.SizeChange).ToList();
                    break;
            }
        }

        private void ClearAll()
        {
            _oldVersionPath = string.Empty;
            _newVersionPath = string.Empty;
            _oldVersionInfo = null;
            _newVersionInfo = null;
            _diffList.Clear();
            _hasDiff = false;
        }

        private void ExportReport()
        {
            var path = EditorUtility.SaveFilePanel("Export Diff Report", "", "version_diff_report.md", "md");
            if (string.IsNullOrEmpty(path)) return;
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Version Diff Report");
            sb.AppendLine();
            sb.AppendLine($"**Old Version:** {_oldVersionInfo.Version} ({_oldVersionPath})");
            sb.AppendLine($"**New Version:** {_newVersionInfo.Version} ({_newVersionPath})");
            sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("## Summary");
            sb.AppendLine($"- Added: {_addedCount}");
            sb.AppendLine($"- Removed: {_removedCount}");
            sb.AppendLine($"- Modified: {_modifiedCount}");
            sb.AppendLine($"- Unchanged: {_unchangedCount}");
            sb.AppendLine($"- Total Size Change: {(_totalSizeChange >= 0 ? "+" : "")}{FormatSize(_totalSizeChange)}");
            sb.AppendLine();

            WriteSection("Added Bundles", DiffStatus.Added);
            WriteSection("Removed Bundles", DiffStatus.Removed);
            WriteSection("Modified Bundles", DiffStatus.Modified);
            
            System.IO.File.WriteAllText(path, sb.ToString());
            EditorUtility.DisplayDialog("Success", $"Report exported to:\n{path}", "OK");
            return;

            void WriteSection(string secTitle, DiffStatus status)
            {
                var items = _diffList.Where(d => d.Status == status).ToList();
                if (items.Count == 0)
                {
                    return;
                }
                
                sb.AppendLine($"## {secTitle} ({items.Count})");
                sb.AppendLine();
                foreach (var item in items)
                {
                    sb.AppendLine($"### {item.BundlePath}");
                    if (item.OldBundle != null) sb.AppendLine($"- Old Hash: {item.OldBundle.Hash}");
                    if (item.NewBundle != null) sb.AppendLine($"- New Hash: {item.NewBundle.Hash}");
                    if (item.SizeChange != 0) sb.AppendLine($"- Size Change: {(item.SizeChange >= 0 ? "+" : "")}{FormatSize(item.SizeChange)}");
                    if (item.AddedAssets.Count > 0) sb.AppendLine($"- Added Assets: {string.Join(", ", item.AddedAssets)}");
                    if (item.RemovedAssets.Count > 0) sb.AppendLine($"- Removed Assets: {string.Join(", ", item.RemovedAssets)}");
                    sb.AppendLine();
                }
            }
        }

        private static bool HasDetails(BundleDiffInfo diff)
        {
            return diff.HashChanged || 
                   diff.AddedAssets.Count > 0 || 
                   diff.RemovedAssets.Count > 0 ||
                   diff.AddedDependencies.Count > 0 || 
                   diff.RemovedDependencies.Count > 0;
        }

        private static Color GetStatusColor(DiffStatus status)
        {
            return status switch
            {
                DiffStatus.Added => new Color(0.2f, 0.8f, 0.2f),
                DiffStatus.Removed => new Color(0.9f, 0.3f, 0.3f),
                DiffStatus.Modified => new Color(0.9f, 0.7f, 0.2f),
                DiffStatus.Unchanged => Color.gray,
                _ => Color.white
            };
        }

        private static Color GetStatusBackgroundColor(DiffStatus status)
        {
            return status switch
            {
                DiffStatus.Added => new Color(0.2f, 0.4f, 0.2f, 0.3f),
                DiffStatus.Removed => new Color(0.4f, 0.2f, 0.2f, 0.3f),
                DiffStatus.Modified => new Color(0.4f, 0.35f, 0.2f, 0.3f),
                DiffStatus.Unchanged => new Color(0.3f, 0.3f, 0.3f, 0.3f),
                _ => Color.white
            };
        }

        private static string GetStatusText(DiffStatus status)
        {
            return status switch
            {
                DiffStatus.Added => "ADDED",
                DiffStatus.Removed => "REMOVED",
                DiffStatus.Modified => "MODIFIED",
                DiffStatus.Unchanged => "UNCHANGED",
                _ => "UNKNOWN"
            };
        }

        private static string FormatSize(long bytes)
        {
            var absBytes = Math.Abs(bytes);
            var sizes = new string[] { "B", "KB", "MB", "GB" };
            var order = 0;
            double size = absBytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            var result = $"{size:0.##} {sizes[order]}";
            return bytes < 0 ? $"-{result}" : result;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            var pix = new Color[width * height];
            for (var i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }
            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        
        public static void DecryptVersionFile()
        {
            try
            {
                var filePath = EditorUtility.OpenFilePanelWithFilters("Open Version File", PathUtils.GetLiteQuarkRootPath(string.Empty), new string[] { "Version File", "txt" });
                if (File.Exists(filePath))
                {
                    var binaryData = File.ReadAllBytes(filePath);
                    var packInfo = VersionPackInfo.FromBinaryData(binaryData);
                    var jsonData = packInfo.ToJsonData();
                    var jsonPath = Path.ChangeExtension(filePath, "json");
                    File.WriteAllText(jsonPath, jsonData);
                    LEditorLog.Info($"Decrypt Version Success : {jsonPath}");
                    AssetDatabase.Refresh();
                    LiteEditorUtils.OpenFolder(Path.GetDirectoryName(jsonPath));
                }
            }
            catch (Exception ex)
            {
                LEditorLog.Error(ex.Message);
            }
        }
    }
}