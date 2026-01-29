using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LiteQuark.Editor.LoadProfiler
{
    internal sealed class ProfilerWindow : EditorWindow
    {
        private TreeViewState _treeState;
        private ProfilerTreeView _treeView;
        private ProfilerTimelineView _timelineView;
        private SearchField _searchField;
        private ProfilerTreeItem _selectedItem;
        private Vector2 _detailScrollPos;

        private bool _autoRefresh = true;
        private float _refreshTimer;
        private const float RefreshInterval = 0.5f;

        private int _selectedViewMode;
        private readonly string[] _viewModeOptions = { "Sessions", "Records", "Hotspots", "Timeline" };

        private bool _showAllSessions;

        [MenuItem("Lite/Asset/Profiler")]
        private static void ShowWindow()
        {
            var window = GetWindow<ProfilerWindow>("Profiler");
            window.minSize = new Vector2(1000, 600);
            window.Show();
        }

        private void OnEnable()
        {
            _treeState ??= new TreeViewState();
            _treeView = new ProfilerTreeView(_treeState);
            _timelineView = new ProfilerTimelineView();
            _searchField = new SearchField();

            _treeView.OnItemSelectionChanged += OnTreeViewItemSelected;
            _treeView.OnMultipleItemsSelected += OnTreeViewMultipleItemsSelected;
            _timelineView.OnRecordSelected += OnTimelineRecordSelected;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnTreeViewItemSelected(ProfilerTreeItem item)
        {
            _selectedItem = item;
            _detailScrollPos = Vector2.zero;

            // 如果选中了 Session，更新时间线视图（仅在非多选模式下）
            if (item != null && item.LoadType == Runtime.AssetLoadEventType.Session && !_showAllSessions)
            {
                var sessions = ProfilerCore.Instance.GetAllSessions();
                foreach (var session in sessions)
                {
                    if (session.SessionId == item.SessionId)
                    {
                        _timelineView.SetSession(session);
                        break;
                    }
                }
            }
        }

        private void OnTreeViewMultipleItemsSelected(System.Collections.Generic.List<ProfilerTreeItem> items)
        {
            if (items == null || items.Count == 0) return;

            // 筛选出 Session 类型的项目
            var sessionItems = items.FindAll(i => i.LoadType == Runtime.AssetLoadEventType.Session);
            if (sessionItems.Count > 1)
            {
                // 多个 Session 被选中，更新时间线视图为多 Session 模式
                var allSessions = ProfilerCore.Instance.GetAllSessions();
                var selectedSessions = new System.Collections.Generic.List<ProfilerSession>();

                foreach (var sessionItem in sessionItems)
                {
                    var session = allSessions.Find(s => s.SessionId == sessionItem.SessionId);
                    if (session != null)
                    {
                        selectedSessions.Add(session);
                    }
                }

                if (selectedSessions.Count > 1)
                {
                    _timelineView.SetSessions(selectedSessions);
                }
            }
        }

        private void OnTimelineRecordSelected(ProfilerRecord record)
        {
            if (record == null) return;

            // 创建一个临时的 TreeItem 用于显示详情
            _selectedItem = new ProfilerTreeItem
            {
                displayName = Runtime.PathUtils.GetFileName(record.DisplayPath),
                Path = record.DisplayPath,
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
            _detailScrollPos = Vector2.zero;

            // 联动选中 TreeView 中的对应项目
            _treeView?.SelectByRecordId(record.RecordId);

            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingPlayMode)
            {
                _treeView?.Reload();
                Repaint();
            }
        }

        private void Update()
        {
            if (_autoRefresh && Application.isPlaying)
            {
                _refreshTimer += Time.deltaTime;
                if (_refreshTimer >= RefreshInterval)
                {
                    _refreshTimer = 0;
                    _treeView?.Reload();
                    Repaint();
                }
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                DrawNotPlayingMessage();
                return;
            }

            DrawStatistics();

            using (new EditorGUILayout.HorizontalScope())
            {
                // 时间线模式使用不同的布局
                if (_selectedViewMode == 3) // Timeline
                {
                    DrawTimelineView();
                }
                else
                {
                    DrawTreeView();
                }
                DrawDetailPanel();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Enable/Disable profiler
                var isEnabled = Application.isPlaying && ProfilerCore.Instance.IsEnabled;
                var newEnabled = GUILayout.Toggle(isEnabled, "Enable Profiler", EditorStyles.toolbarButton, GUILayout.Width(100));
                if (newEnabled != isEnabled && Application.isPlaying)
                {
                    ProfilerCore.Instance.SetEnabled(newEnabled);
                }

                GUILayout.Space(10);

                // View mode
                EditorGUI.BeginChangeCheck();
                _selectedViewMode = EditorGUILayout.Popup(_selectedViewMode, _viewModeOptions, EditorStyles.toolbarPopup, GUILayout.Width(80));
                if (EditorGUI.EndChangeCheck())
                {
                    _treeView.CurrentViewMode = (ProfilerTreeView.ViewMode)_selectedViewMode;
                    _treeView.Reload();
                }

                GUILayout.Space(10);

                // Auto refresh
                _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(90));

                // Manual refresh
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    _treeView?.Reload();
                }

                GUILayout.Space(10);

                // Clear
                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    if (Application.isPlaying)
                    {
                        ProfilerCore.Instance.Clear();
                        _treeView?.Reload();
                    }
                }

                GUILayout.FlexibleSpace();

                // Export
                if (GUILayout.Button("Export", EditorStyles.toolbarDropDown, GUILayout.Width(60)))
                {
                    ShowExportMenu();
                }
            }
        }

        private void ShowExportMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Export Sessions (JSON)"), false, ExportSessionsJson);
            menu.AddItem(new GUIContent("Export Records (CSV)"), false, ExportRecordsCsv);
            menu.AddItem(new GUIContent("Export Statistics (TXT)"), false, ExportStatistics);
            menu.AddItem(new GUIContent("Export Timeline (HTML)"), false, ExportTimelineHtml);
            menu.ShowAsContext();
        }

        private void ExportSessionsJson()
        {
            var sessions = ProfilerCore.Instance.GetAllSessions();
            if (sessions.Count == 0)
            {
                EditorUtility.DisplayDialog("Export", "No sessions to export.", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanel("Export Sessions", "", "profiler_sessions", "json");
            if (!string.IsNullOrEmpty(path))
            {
                ProfilerExportUtils.ExportToJson(sessions, path);
            }
        }

        private void ExportRecordsCsv()
        {
            var records = ProfilerCore.Instance.GetAllRecords();
            if (records.Count == 0)
            {
                EditorUtility.DisplayDialog("Export", "No records to export.", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanel("Export Records", "", "profiler_records", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                ProfilerExportUtils.ExportToCsv(records, path);
            }
        }

        private void ExportStatistics()
        {
            var stats = ProfilerCore.Instance.GetStatistics();
            var path = EditorUtility.SaveFilePanel("Export Statistics", "", "profiler_statistics", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                ProfilerExportUtils.ExportStatistics(stats, path);
            }
        }

        private void ExportTimelineHtml()
        {
            var sessions = ProfilerCore.Instance.GetAllSessions();
            if (sessions.Count == 0)
            {
                EditorUtility.DisplayDialog("Export", "No sessions to export.", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanel("Export Timeline", "", "profiler_timeline", "html");
            if (!string.IsNullOrEmpty(path))
            {
                ProfilerExportUtils.ExportTimelineHtml(sessions, path);
            }
        }

        private void DrawNotPlayingMessage()
        {
            var rect = new Rect(0, 20, position.width, position.height - 20);
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));
            GUI.Label(rect, "Enter Play Mode to start profiling", ProfilerStyles.NotPlayingStyle);
        }

        private void DrawStatistics()
        {
            var stats = ProfilerCore.Instance.GetStatistics();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.Height(50)))
            {
                DrawStatItem("Records", stats.TotalRecordCount.ToString());
                DrawStatItem("Bundles", $"{stats.BundleCount} (L:{stats.LocalBundleCount} R:{stats.RemoteBundleCount} C:{stats.CachedBundleCount})");
                DrawStatItem("Assets", stats.AssetCount.ToString());
                DrawStatItem("Avg Bundle", $"{stats.AverageBundleDuration:F1}ms");
                DrawStatItem("Avg Asset", $"{stats.AverageAssetDuration:F1}ms");
                DrawStatItem("Total Size", LiteEditorUtils.GetSizeString(stats.TotalSize));
                DrawStatItem("Failed", stats.FailedCount.ToString(), stats.FailedCount > 0 ? ProfilerStyles.ErrorColor : ProfilerStyles.SuccessColor);
            }
        }

        private void DrawStatItem(string label, string value, Color? color = null)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(120)))
            {
                EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
                using (new ColorScope(color ?? Color.white))
                {
                    EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
                }
            }
        }

        private void DrawTreeView()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                // Search field
                var searchRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                _treeView.searchString = _searchField.OnGUI(searchRect, _treeView.searchString);

                // Tree view
                var treeRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                _treeView.OnGUI(treeRect);
            }
        }

        private void DrawTimelineView()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                // 工具栏
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    _showAllSessions = GUILayout.Toggle(_showAllSessions, "Show All Sessions", EditorStyles.toolbarButton, GUILayout.Width(120));
                    if (EditorGUI.EndChangeCheck())
                    {
                        UpdateTimelineView();
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Scroll: Zoom | Middle Mouse: Pan | Double-click: Reset", EditorStyles.miniLabel);
                }

                // 时间线视图
                var timelineRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                _timelineView.OnGUI(timelineRect);
            }
        }

        private void UpdateTimelineView()
        {
            if (_showAllSessions)
            {
                var sessions = ProfilerCore.Instance.GetRecentSessions(100);
                _timelineView.SetSessions(sessions);
            }
            else if (_selectedItem != null && _selectedItem.LoadType == Runtime.AssetLoadEventType.Session)
            {
                var sessions = ProfilerCore.Instance.GetAllSessions();
                foreach (var session in sessions)
                {
                    if (session.SessionId == _selectedItem.SessionId)
                    {
                        _timelineView.SetSession(session);
                        break;
                    }
                }
            }
            else
            {
                _timelineView.Clear();
            }
        }

        private void DrawDetailPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(300), GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);

                if (_selectedItem == null)
                {
                    EditorGUILayout.LabelField("Select an item to view details", EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);

                EditorGUILayout.LabelField("Name", _selectedItem.displayName);
                EditorGUILayout.LabelField("Type", _selectedItem.LoadType.ToString());
                EditorGUILayout.LabelField("Source", _selectedItem.LoadSource.ToString());
                EditorGUILayout.LabelField("Duration", $"{_selectedItem.Duration:F2}ms");
                EditorGUILayout.LabelField("Status", _selectedItem.IsSuccess ? "Success" : "Failed");

                if (_selectedItem.Size > 0)
                {
                    EditorGUILayout.LabelField("Size", LiteEditorUtils.GetSizeString(_selectedItem.Size));
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Path", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(_selectedItem.Path, EditorStyles.textField, GUILayout.Height(40));

                if (_selectedItem.Dependencies != null && _selectedItem.Dependencies.Length > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Dependencies ({_selectedItem.Dependencies.Length})", EditorStyles.boldLabel);

                    foreach (var dep in _selectedItem.Dependencies)
                    {
                        EditorGUILayout.LabelField(Runtime.PathUtils.GetFileName(dep), EditorStyles.miniLabel);
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("IDs", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Record ID", _selectedItem.RecordId.ToString());
                EditorGUILayout.LabelField("Session ID", _selectedItem.SessionId.ToString());

                EditorGUILayout.EndScrollView();
            }
        }
    }
}
