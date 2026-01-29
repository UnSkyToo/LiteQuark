using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LiteQuark.Runtime;

namespace LiteQuark.Editor.LoadProfiler
{
    /// <summary>
    /// 时间线视图，以甘特图形式展示资源加载过程
    /// </summary>
    internal sealed class ProfilerTimelineView
    {
        public event Action<ProfilerRecord> OnRecordSelected;

        private const float RowHeight = 20f;
        private const float HeaderHeight = 30f;
        private const float LeftMargin = 150f;
        private const float MinBarWidth = 2f;
        private const float SessionHeaderHeight = 24f;

        private Vector2 _scrollPosition;
        private float _zoom = 1f;
        private float _panOffset;
        private ProfilerSession _currentSession;
        private ProfilerRecord _selectedRecord;
        private ProfilerRecord _hoveredRecord;

        // 多 Session 支持
        private readonly List<ProfilerSession> _sessions = new();
        private bool _multiSessionMode;

        // 时间范围
        private long _timeRangeStart;
        private long _timeRangeEnd;
        private float _timeRangeDuration;

        // 缓存的绘制数据
        private readonly List<TimelineRow> _rows = new();
        private readonly List<SessionGroup> _sessionGroups = new();
        private readonly List<ParallelRegion> _parallelRegions = new();
        private readonly HashSet<long> _criticalPathRecordIds = new();

        private struct TimelineRow
        {
            public ProfilerRecord Record;
            public float StartX;
            public float Width;
            public int RowIndex;
            public int SessionIndex;
        }

        private struct SessionGroup
        {
            public ProfilerSession Session;
            public float StartY;
            public float Height;
            public int RowCount;
        }

        private struct ParallelRegion
        {
            public long StartTime;
            public long EndTime;
            public int ConcurrentCount;
            public int SessionIndex;
        }

        public void SetSession(ProfilerSession session)
        {
            if (_currentSession == session && !_multiSessionMode) return;

            _currentSession = session;
            _sessions.Clear();
            if (session != null) _sessions.Add(session);
            _multiSessionMode = false;
            _selectedRecord = null;
            _hoveredRecord = null;
            RebuildRows();
        }

        /// <summary>
        /// 设置多个 Session（分组显示模式）
        /// </summary>
        public void SetSessions(List<ProfilerSession> sessions)
        {
            _sessions.Clear();
            _sessions.AddRange(sessions);
            _currentSession = _sessions.Count > 0 ? _sessions[0] : null;
            _multiSessionMode = _sessions.Count > 1;
            _selectedRecord = null;
            _hoveredRecord = null;
            RebuildRows();
        }

        public void Clear()
        {
            _currentSession = null;
            _sessions.Clear();
            _multiSessionMode = false;
            _selectedRecord = null;
            _hoveredRecord = null;
            _rows.Clear();
            _sessionGroups.Clear();
            _parallelRegions.Clear();
            _criticalPathRecordIds.Clear();
        }

        private void RebuildRows()
        {
            _rows.Clear();
            _sessionGroups.Clear();
            _parallelRegions.Clear();
            _criticalPathRecordIds.Clear();

            if (_sessions.Count == 0) return;

            // 计算全局时间范围
            _timeRangeStart = long.MaxValue;
            _timeRangeEnd = long.MinValue;
            foreach (var session in _sessions)
            {
                if (session.Records.Count == 0) continue;
                if (session.RequestTimestamp < _timeRangeStart) _timeRangeStart = session.RequestTimestamp;
                if (session.CompleteTimestamp > _timeRangeEnd) _timeRangeEnd = session.CompleteTimestamp;
            }

            // 确保有有效的时间范围
            if (_timeRangeEnd <= _timeRangeStart)
            {
                _timeRangeEnd = _timeRangeStart + 1;
            }
            _timeRangeDuration = _timeRangeEnd - _timeRangeStart;

            // 为每个 Session 构建行
            var currentY = 0f;
            for (var sessionIndex = 0; sessionIndex < _sessions.Count; sessionIndex++)
            {
                var session = _sessions[sessionIndex];
                if (session.Records.Count == 0) continue;

                var sessionStartY = currentY;
                if (_multiSessionMode)
                {
                    currentY += SessionHeaderHeight;
                }

                // 按开始时间排序
                var sortedRecords = new List<ProfilerRecord>(session.Records);
                sortedRecords.Sort((a, b) => a.StartTimestamp.CompareTo(b.StartTimestamp));

                // 分配行（简单的贪心算法，避免重叠）
                var rowEndTimes = new List<long>();

                foreach (var record in sortedRecords)
                {
                    var rowIndex = FindAvailableRow(rowEndTimes, record.StartTimestamp);
                    if (rowIndex >= rowEndTimes.Count)
                    {
                        rowEndTimes.Add(record.EndTimestamp);
                    }
                    else
                    {
                        rowEndTimes[rowIndex] = record.EndTimestamp;
                    }

                    _rows.Add(new TimelineRow
                    {
                        Record = record,
                        RowIndex = rowIndex,
                        SessionIndex = sessionIndex
                    });
                }

                var rowCount = rowEndTimes.Count;
                var sessionHeight = rowCount * RowHeight + (_multiSessionMode ? SessionHeaderHeight : 0);

                _sessionGroups.Add(new SessionGroup
                {
                    Session = session,
                    StartY = sessionStartY,
                    Height = sessionHeight,
                    RowCount = rowCount
                });

                // 计算并行区域
                CalculateParallelRegions(sortedRecords, sessionIndex);

                // 计算关键路径
                CalculateCriticalPath(sortedRecords);

                currentY = sessionStartY + sessionHeight + (_multiSessionMode ? 5 : 0);
            }
        }

        private void CalculateCriticalPath(List<ProfilerRecord> records)
        {
            // 多 Session 模式下跳过关键路径计算（性能考虑）
            if (_multiSessionMode || records.Count == 0 || records.Count > 200) return;

            // 找到结束时间最晚的记录
            ProfilerRecord lastRecord = null;
            var latestEnd = long.MinValue;

            foreach (var record in records)
            {
                if (record.EndTimestamp > latestEnd)
                {
                    latestEnd = record.EndTimestamp;
                    lastRecord = record;
                }
            }

            if (lastRecord == null) return;

            // 按结束时间排序，用于二分查找优化
            var sortedByEnd = new List<ProfilerRecord>(records);
            sortedByEnd.Sort((a, b) => a.EndTimestamp.CompareTo(b.EndTimestamp));

            // 从最后一个记录开始，回溯找到关键路径
            var currentRecord = lastRecord;
            var maxIterations = 100; // 防止无限循环
            while (currentRecord != null && maxIterations-- > 0)
            {
                _criticalPathRecordIds.Add(currentRecord.RecordId);

                // 使用二分查找找到结束时间 <= 当前开始时间的最后一个记录
                var targetTime = currentRecord.StartTimestamp;
                ProfilerRecord predecessor = null;

                // 二分查找
                var left = 0;
                var right = sortedByEnd.Count - 1;
                while (left <= right)
                {
                    var mid = (left + right) / 2;
                    if (sortedByEnd[mid].EndTimestamp <= targetTime)
                    {
                        predecessor = sortedByEnd[mid];
                        left = mid + 1;
                    }
                    else
                    {
                        right = mid - 1;
                    }
                }

                // 确保不选择自己
                if (predecessor != null && predecessor.RecordId == currentRecord.RecordId)
                {
                    predecessor = null;
                    // 向前找一个
                    for (var i = sortedByEnd.IndexOf(currentRecord) - 1; i >= 0; i--)
                    {
                        if (sortedByEnd[i].EndTimestamp <= targetTime)
                        {
                            predecessor = sortedByEnd[i];
                            break;
                        }
                    }
                }

                currentRecord = predecessor;
            }
        }

        private void CalculateParallelRegions(List<ProfilerRecord> records, int sessionIndex)
        {
            // 多 Session 模式下跳过并行区域计算（性能考虑）
            if (_multiSessionMode || records.Count < 2 || records.Count > 500) return;

            // 收集所有时间点
            var events = new List<(long time, bool isStart)>();
            foreach (var record in records)
            {
                events.Add((record.StartTimestamp, true));
                events.Add((record.EndTimestamp, false));
            }
            events.Sort((a, b) => a.time != b.time ? a.time.CompareTo(b.time) : (a.isStart ? -1 : 1));

            // 扫描线算法计算并行区域
            var concurrentCount = 0;
            var regionStart = 0L;

            foreach (var evt in events)
            {
                if (concurrentCount >= 2 && evt.time > regionStart)
                {
                    _parallelRegions.Add(new ParallelRegion
                    {
                        StartTime = regionStart,
                        EndTime = evt.time,
                        ConcurrentCount = concurrentCount,
                        SessionIndex = sessionIndex
                    });
                }

                if (evt.isStart)
                {
                    concurrentCount++;
                    if (concurrentCount == 2)
                    {
                        regionStart = evt.time;
                    }
                }
                else
                {
                    if (concurrentCount >= 2)
                    {
                        regionStart = evt.time;
                    }
                    concurrentCount--;
                }
            }
        }

        private int FindAvailableRow(List<long> rowEndTimes, long startTime)
        {
            for (var i = 0; i < rowEndTimes.Count; i++)
            {
                if (rowEndTimes[i] <= startTime)
                {
                    return i;
                }
            }
            return rowEndTimes.Count;
        }

        public void OnGUI(Rect rect)
        {
            if (_sessions.Count == 0)
            {
                DrawEmptyMessage(rect);
                return;
            }

            // 处理输入
            HandleInput(rect);

            // 绘制背景
            EditorGUI.DrawRect(rect, ProfilerStyles.TimelineBackground);

            // 计算内容区域
            var headerRect = new Rect(rect.x, rect.y, rect.width, HeaderHeight);
            var contentRect = new Rect(rect.x, rect.y + HeaderHeight, rect.width, rect.height - HeaderHeight);

            // 绘制时间轴刻度
            DrawTimeAxis(headerRect);

            // 计算内容高度
            var contentHeight = 20f;
            if (_sessionGroups.Count > 0)
            {
                var lastGroup = _sessionGroups[_sessionGroups.Count - 1];
                contentHeight = lastGroup.StartY + lastGroup.Height + 20;
            }
            var scrollViewRect = new Rect(0, 0, contentRect.width - 16, contentHeight);

            _scrollPosition = GUI.BeginScrollView(contentRect, _scrollPosition, scrollViewRect);
            DrawRecordBars(scrollViewRect);
            GUI.EndScrollView();

            // 绘制悬停提示
            if (_hoveredRecord != null)
            {
                DrawTooltip(rect);
            }
        }

        private void DrawEmptyMessage(Rect rect)
        {
            EditorGUI.DrawRect(rect, ProfilerStyles.TimelineBackground);
            GUI.Label(rect, "Select a session to view timeline", ProfilerStyles.NotPlayingStyle);
        }

        private void DrawTimeAxis(Rect rect)
        {
            // 绘制背景
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));

            // 计算可见时间范围
            var timelineWidth = rect.width - LeftMargin;
            var visibleDuration = _timeRangeDuration / _zoom;
            var visibleStart = _timeRangeStart + (long)(_panOffset * _timeRangeDuration);

            // 计算刻度间隔（自适应）
            var interval = CalculateTickInterval(visibleDuration, timelineWidth);

            // 绘制刻度线和标签
            var startTick = (long)Math.Ceiling((double)visibleStart / interval) * interval;

            for (var tick = startTick; tick < visibleStart + visibleDuration; tick += interval)
            {
                var x = TimeToX(tick, rect.x + LeftMargin, timelineWidth);
                if (x < rect.x + LeftMargin || x > rect.xMax) continue;

                // 绘制刻度线
                EditorGUI.DrawRect(new Rect(x, rect.y, 1, rect.height), ProfilerStyles.TimelineGridLine);

                // 绘制时间标签
                var timeLabel = FormatTime(tick - _timeRangeStart);
                var labelRect = new Rect(x - 20, rect.y + 5, 40, 20);
                GUI.Label(labelRect, timeLabel, EditorStyles.miniLabel);
            }

            // 绘制总时长标签
            var durationLabel = $"Total: {_timeRangeDuration:F0}ms";
            GUI.Label(new Rect(rect.x + 5, rect.y + 5, 100, 20), durationLabel, EditorStyles.boldLabel);
        }

        private long CalculateTickInterval(float visibleDuration, float width)
        {
            // 目标：每 80-120 像素一个刻度
            var targetTickCount = width / 100f;
            var rawInterval = visibleDuration / targetTickCount;

            // 取整到合适的值（1, 2, 5, 10, 20, 50, 100, ...）
            var magnitude = (long)Math.Pow(10, Math.Floor(Math.Log10(rawInterval)));
            var normalized = rawInterval / magnitude;

            long multiplier;
            if (normalized < 1.5) multiplier = 1;
            else if (normalized < 3.5) multiplier = 2;
            else if (normalized < 7.5) multiplier = 5;
            else multiplier = 10;

            return Math.Max(1, magnitude * multiplier);
        }

        private float TimeToX(long timestamp, float startX, float width)
        {
            var normalizedTime = (timestamp - _timeRangeStart - _panOffset * _timeRangeDuration) / _timeRangeDuration;
            return startX + normalizedTime * width * _zoom;
        }

        private string FormatTime(float ms)
        {
            if (ms < 1000) return $"{ms:F0}ms";
            return $"{ms / 1000:F1}s";
        }

        private void DrawRecordBars(Rect scrollViewRect)
        {
            var timelineWidth = scrollViewRect.width - LeftMargin;
            _hoveredRecord = null;

            // 绘制并行区域高亮
            DrawParallelRegions(scrollViewRect, timelineWidth);

            // 绘制左侧名称区域背景（防止缩放时条形图与名称重叠）
            EditorGUI.DrawRect(new Rect(0, 0, LeftMargin, scrollViewRect.height), ProfilerStyles.TimelineBackground);

            // 绘制 Session 分组头
            if (_multiSessionMode)
            {
                foreach (var group in _sessionGroups)
                {
                    var headerRect = new Rect(0, group.StartY, scrollViewRect.width, SessionHeaderHeight);
                    EditorGUI.DrawRect(headerRect, new Color(0.25f, 0.25f, 0.3f, 1f));

                    var sessionName = PathUtils.GetFileName(group.Session.RequestPath);
                    var labelRect = new Rect(5, group.StartY + 4, LeftMargin - 10, SessionHeaderHeight - 8);
                    GUI.Label(labelRect, $"[{group.Session.SessionId}] {sessionName}", EditorStyles.boldLabel);

                    var durationRect = new Rect(LeftMargin, group.StartY + 4, 100, SessionHeaderHeight - 8);
                    GUI.Label(durationRect, $"{group.Session.TotalDuration:F1}ms", EditorStyles.miniLabel);
                }
            }

            foreach (var row in _rows)
            {
                var record = row.Record;
                var group = _sessionGroups[row.SessionIndex];
                var baseY = group.StartY + (_multiSessionMode ? SessionHeaderHeight : 0);
                var y = baseY + row.RowIndex * RowHeight + 2;

                // 计算条形位置
                var startX = TimeToX(record.StartTimestamp, LeftMargin, timelineWidth);
                var endX = TimeToX(record.EndTimestamp, LeftMargin, timelineWidth);
                var barWidth = Mathf.Max(MinBarWidth, endX - startX);

                // 裁剪到可见区域
                if (startX > scrollViewRect.width || endX < LeftMargin) continue;

                var barRect = new Rect(startX, y, barWidth, RowHeight - 4);

                // 绘制条形
                var color = ProfilerStyles.GetLoadTypeColor(record.Type);
                if (!record.IsSuccess) color = ProfilerStyles.ErrorColor;
                if (record == _selectedRecord) color = Color.Lerp(color, Color.white, 0.3f);

                EditorGUI.DrawRect(barRect, color);

                // 绘制关键路径标记
                var isCriticalPath = _criticalPathRecordIds.Contains(record.RecordId);
                if (isCriticalPath)
                {
                    // 绘制金色边框表示关键路径
                    DrawRectOutline(barRect, new Color(1f, 0.8f, 0f, 1f));
                    // 绘制顶部高亮条
                    EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width, 2), new Color(1f, 0.8f, 0f, 1f));
                }

                // 绘制边框（选中/悬停）
                if (record == _hoveredRecord || record == _selectedRecord)
                {
                    DrawRectOutline(barRect, Color.white);
                }

                // 绘制标签（如果空间足够）
                if (barWidth > 30)
                {
                    var labelRect = new Rect(barRect.x + 2, barRect.y, barRect.width - 4, barRect.height);
                    var label = PathUtils.GetFileName(record.DisplayPath);
                    GUI.Label(labelRect, label, EditorStyles.miniLabel);
                }

                // 绘制左侧名称（仅单 Session 模式）
                if (!_multiSessionMode)
                {
                    var nameRect = new Rect(5, y, LeftMargin - 10, RowHeight - 4);
                    var displayName = PathUtils.GetFileName(record.DisplayPath);
                    if (displayName.Length > 18) displayName = displayName.Substring(0, 15) + "...";
                    GUI.Label(nameRect, displayName, EditorStyles.miniLabel);
                }

                // 检测悬停
                if (barRect.Contains(Event.current.mousePosition))
                {
                    _hoveredRecord = record;
                }

                // 检测点击
                if (Event.current.type == EventType.MouseDown && barRect.Contains(Event.current.mousePosition))
                {
                    _selectedRecord = record;
                    OnRecordSelected?.Invoke(record);
                    Event.current.Use();
                }
            }

            // 绘制网格线
            DrawGridLines(scrollViewRect, timelineWidth);
        }

        private void DrawParallelRegions(Rect scrollViewRect, float timelineWidth)
        {
            foreach (var region in _parallelRegions)
            {
                var group = _sessionGroups[region.SessionIndex];
                var baseY = group.StartY + (_multiSessionMode ? SessionHeaderHeight : 0);

                var startX = TimeToX(region.StartTime, LeftMargin, timelineWidth);
                var endX = TimeToX(region.EndTime, LeftMargin, timelineWidth);

                // 裁剪到可见区域
                if (startX > scrollViewRect.width || endX < LeftMargin) continue;

                startX = Mathf.Max(startX, LeftMargin);
                endX = Mathf.Min(endX, scrollViewRect.width);

                var regionRect = new Rect(startX, baseY, endX - startX, group.RowCount * RowHeight);

                // 根据并发数量调整颜色强度
                var alpha = Mathf.Clamp(0.1f + region.ConcurrentCount * 0.05f, 0.1f, 0.3f);
                var color = new Color(0.2f, 0.6f, 1f, alpha);
                EditorGUI.DrawRect(regionRect, color);
            }
        }

        private void DrawGridLines(Rect rect, float timelineWidth)
        {
            // 绘制垂直网格线（与时间轴刻度对齐）
            var visibleDuration = _timeRangeDuration / _zoom;
            var interval = CalculateTickInterval(visibleDuration, timelineWidth);
            var visibleStart = _timeRangeStart + (long)(_panOffset * _timeRangeDuration);
            var startTick = (long)Math.Ceiling((double)visibleStart / interval) * interval;

            for (var tick = startTick; tick < visibleStart + visibleDuration; tick += interval)
            {
                var x = TimeToX(tick, LeftMargin, timelineWidth);
                if (x < LeftMargin || x > rect.width) continue;

                EditorGUI.DrawRect(new Rect(x, 0, 1, rect.height), new Color(0.3f, 0.3f, 0.3f, 0.5f));
            }
        }

        private void DrawRectOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color);
        }

        private void DrawTooltip(Rect containerRect)
        {
            if (_hoveredRecord == null) return;

            var mousePos = Event.current.mousePosition;
            var tooltipWidth = 220f;
            var tooltipHeight = 90f;
            var lineHeight = 18f;

            // 确保提示框在容器内
            var x = mousePos.x + 15;
            var y = mousePos.y + 15;
            if (x + tooltipWidth > containerRect.xMax) x = mousePos.x - tooltipWidth - 5;
            if (y + tooltipHeight > containerRect.yMax) y = mousePos.y - tooltipHeight - 5;

            var tooltipRect = new Rect(x, y, tooltipWidth, tooltipHeight);

            // 绘制背景
            EditorGUI.DrawRect(tooltipRect, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            DrawRectOutline(tooltipRect, Color.gray);

            // 绘制内容（使用 GUI.Label 而非 GUILayout）
            var labelX = tooltipRect.x + 5;
            var labelY = tooltipRect.y + 5;
            var labelWidth = tooltipRect.width - 10;

            GUI.Label(new Rect(labelX, labelY, labelWidth, lineHeight), PathUtils.GetFileName(_hoveredRecord.DisplayPath), EditorStyles.boldLabel);
            labelY += lineHeight;
            GUI.Label(new Rect(labelX, labelY, labelWidth, lineHeight), $"Type: {_hoveredRecord.Type}", EditorStyles.miniLabel);
            labelY += lineHeight;
            GUI.Label(new Rect(labelX, labelY, labelWidth, lineHeight), $"Duration: {_hoveredRecord.Duration:F1}ms", EditorStyles.miniLabel);
            labelY += lineHeight;
            GUI.Label(new Rect(labelX, labelY, labelWidth, lineHeight), $"Source: {_hoveredRecord.Source}", EditorStyles.miniLabel);
        }

        private void HandleInput(Rect rect)
        {
            var e = Event.current;

            // 滚轮缩放
            if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
            {
                var zoomDelta = -e.delta.y * 0.1f;
                _zoom = Mathf.Clamp(_zoom + zoomDelta * _zoom, 0.1f, 10f);
                e.Use();
            }

            // 中键拖拽平移
            if (e.type == EventType.MouseDrag && e.button == 2 && rect.Contains(e.mousePosition))
            {
                var panDelta = -e.delta.x / (rect.width - LeftMargin) / _zoom;
                _panOffset = Mathf.Clamp(_panOffset + panDelta, 0f, 1f - 1f / _zoom);
                e.Use();
            }

            // 重置视图（双击）
            if (e.type == EventType.MouseDown && e.clickCount == 2 && rect.Contains(e.mousePosition))
            {
                _zoom = 1f;
                _panOffset = 0f;
                e.Use();
            }
        }
    }
}