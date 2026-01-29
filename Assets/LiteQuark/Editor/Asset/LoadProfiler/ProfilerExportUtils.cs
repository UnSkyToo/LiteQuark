using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace LiteQuark.Editor.LoadProfiler
{
    internal static class ProfilerExportUtils
    {
        public static void ExportToJson(List<ProfilerSession> sessions, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"exportTime\": \"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\",");
            sb.AppendLine("  \"sessions\": [");

            for (var i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"sessionId\": {session.SessionId},");
                sb.AppendLine($"      \"requestPath\": \"{EscapeJson(session.RequestPath)}\",");
                sb.AppendLine($"      \"totalDuration\": {session.TotalDuration:F2},");
                sb.AppendLine($"      \"bundleCount\": {session.BundleCount},");
                sb.AppendLine($"      \"assetCount\": {session.AssetCount},");
                sb.AppendLine($"      \"totalSize\": {session.TotalSize},");
                sb.AppendLine($"      \"isSuccess\": {session.IsSuccess.ToString().ToLower()},");
                sb.AppendLine("      \"records\": [");

                for (var j = 0; j < session.Records.Count; j++)
                {
                    var record = session.Records[j];
                    sb.AppendLine("        {");
                    sb.AppendLine($"          \"recordId\": {record.RecordId},");
                    sb.AppendLine($"          \"type\": \"{record.Type}\",");
                    sb.AppendLine($"          \"source\": \"{record.Source}\",");
                    sb.AppendLine($"          \"assetPath\": \"{EscapeJson(record.AssetPath)}\",");
                    sb.AppendLine($"          \"bundlePath\": \"{EscapeJson(record.BundlePath)}\",");
                    sb.AppendLine($"          \"duration\": {record.Duration:F2},");
                    sb.AppendLine($"          \"size\": {record.Size},");
                    sb.AppendLine($"          \"isSuccess\": {record.IsSuccess.ToString().ToLower()},");
                    sb.AppendLine($"          \"errorMessage\": \"{EscapeJson(record.ErrorMessage)}\"");
                    sb.Append("        }");
                    if (j < session.Records.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }

                sb.AppendLine("      ]");
                sb.Append("    }");
                if (i < sessions.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"Report exported to: {filePath}");
        }

        public static void ExportToCsv(List<ProfilerRecord> records, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("RecordId,SessionId,Type,Source,AssetPath,BundlePath,Duration(ms),Size,IsSuccess,ErrorMessage");

            foreach (var record in records)
            {
                sb.AppendLine($"{record.RecordId},{record.SessionId},{record.Type},{record.Source},\"{EscapeCsv(record.AssetPath)}\",\"{EscapeCsv(record.BundlePath)}\",{record.Duration:F2},{record.Size},{record.IsSuccess},\"{EscapeCsv(record.ErrorMessage)}\"");
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"Report exported to: {filePath}");
        }

        public static void ExportStatistics(ProfilerStatistics stats, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Asset Load Profiler Statistics Report");
            sb.AppendLine("======================================");
            sb.AppendLine($"Export Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("Summary");
            sb.AppendLine("-------");
            sb.AppendLine($"Total Records: {stats.TotalRecordCount}");
            sb.AppendLine($"Total Duration: {stats.TotalDuration:F2}ms");
            sb.AppendLine($"Total Size: {LiteEditorUtils.GetSizeString(stats.TotalSize)}");
            sb.AppendLine($"Failed Count: {stats.FailedCount}");
            sb.AppendLine();
            sb.AppendLine("Bundle Statistics");
            sb.AppendLine("-----------------");
            sb.AppendLine($"Total Bundles: {stats.BundleCount}");
            sb.AppendLine($"  - Local: {stats.LocalBundleCount}");
            sb.AppendLine($"  - Remote: {stats.RemoteBundleCount}");
            sb.AppendLine($"  - Cached: {stats.CachedBundleCount}");
            sb.AppendLine($"Average Bundle Duration: {stats.AverageBundleDuration:F2}ms");
            sb.AppendLine();
            sb.AppendLine("Asset Statistics");
            sb.AppendLine("----------------");
            sb.AppendLine($"Total Assets: {stats.AssetCount}");
            sb.AppendLine($"Average Asset Duration: {stats.AverageAssetDuration:F2}ms");
            sb.AppendLine();
            sb.AppendLine("Scene Statistics");
            sb.AppendLine("----------------");
            sb.AppendLine($"Total Scenes: {stats.SceneCount}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"Statistics exported to: {filePath}");
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\"", "\"\"");
        }

        public static void ExportTimelineHtml(List<ProfilerSession> sessions, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine("<title>Asset Load Timeline</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; background: #1e1e1e; color: #fff; margin: 20px; }");
            sb.AppendLine(".timeline { position: relative; margin: 20px 0; }");
            sb.AppendLine(".session { margin-bottom: 30px; }");
            sb.AppendLine(".session-header { font-size: 14px; font-weight: bold; margin-bottom: 5px; color: #4fc3f7; }");
            sb.AppendLine(".timeline-container { position: relative; background: #2d2d2d; border-radius: 4px; overflow: hidden; }");
            sb.AppendLine(".bar { position: absolute; height: 18px; border-radius: 2px; font-size: 10px; color: #fff; overflow: hidden; white-space: nowrap; text-overflow: ellipsis; padding: 2px 4px; box-sizing: border-box; }");
            sb.AppendLine(".bar.bundle { background: #4caf50; }");
            sb.AppendLine(".bar.asset { background: #2196f3; }");
            sb.AppendLine(".bar.scene { background: #9c27b0; }");
            sb.AppendLine(".bar.failed { background: #f44336; }");
            sb.AppendLine(".bar.critical { border: 2px solid #ffc107; }");
            sb.AppendLine(".time-axis { height: 25px; background: #333; position: relative; }");
            sb.AppendLine(".time-tick { position: absolute; font-size: 10px; color: #888; }");
            sb.AppendLine(".legend { margin-top: 20px; }");
            sb.AppendLine(".legend-item { display: inline-block; margin-right: 20px; }");
            sb.AppendLine(".legend-color { display: inline-block; width: 12px; height: 12px; margin-right: 5px; vertical-align: middle; }");
            sb.AppendLine(".tooltip { position: absolute; background: rgba(0,0,0,0.9); padding: 8px; border-radius: 4px; font-size: 11px; pointer-events: none; z-index: 100; display: none; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine($"<h1>Asset Load Timeline</h1>");
            sb.AppendLine($"<p>Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

            foreach (var session in sessions)
            {
                if (session.Records.Count == 0) continue;
                ExportSessionTimeline(sb, session);
            }

            sb.AppendLine("<div class=\"legend\">");
            sb.AppendLine("<span class=\"legend-item\"><span class=\"legend-color\" style=\"background:#4caf50\"></span>Bundle</span>");
            sb.AppendLine("<span class=\"legend-item\"><span class=\"legend-color\" style=\"background:#2196f3\"></span>Asset</span>");
            sb.AppendLine("<span class=\"legend-item\"><span class=\"legend-color\" style=\"background:#9c27b0\"></span>Scene</span>");
            sb.AppendLine("<span class=\"legend-item\"><span class=\"legend-color\" style=\"background:#f44336\"></span>Failed</span>");
            sb.AppendLine("<span class=\"legend-item\"><span class=\"legend-color\" style=\"border:2px solid #ffc107;width:8px;height:8px\"></span>Critical Path</span>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body></html>");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"Timeline exported to: {filePath}");
        }

        private static void ExportSessionTimeline(StringBuilder sb, ProfilerSession session)
        {
            var timeStart = session.RequestTimestamp;
            var timeEnd = session.CompleteTimestamp;
            var duration = timeEnd - timeStart;
            if (duration <= 0) duration = 1;

            // 计算行分配
            var sortedRecords = new List<ProfilerRecord>(session.Records);
            sortedRecords.Sort((a, b) => a.StartTimestamp.CompareTo(b.StartTimestamp));

            var rowEndTimes = new List<long>();
            var recordRows = new Dictionary<long, int>();

            foreach (var record in sortedRecords)
            {
                var rowIndex = 0;
                for (var i = 0; i < rowEndTimes.Count; i++)
                {
                    if (rowEndTimes[i] <= record.StartTimestamp)
                    {
                        rowIndex = i;
                        break;
                    }
                    rowIndex = i + 1;
                }

                if (rowIndex >= rowEndTimes.Count)
                    rowEndTimes.Add(record.EndTimestamp);
                else
                    rowEndTimes[rowIndex] = record.EndTimestamp;

                recordRows[record.RecordId] = rowIndex;
            }

            var rowCount = rowEndTimes.Count;
            var containerHeight = rowCount * 22 + 30;

            sb.AppendLine("<div class=\"session\">");
            sb.AppendLine($"<div class=\"session-header\">[{session.SessionId}] {Runtime.PathUtils.GetFileName(session.RequestPath)} - {session.TotalDuration:F1}ms</div>");
            sb.AppendLine($"<div class=\"timeline-container\" style=\"height:{containerHeight}px\">");

            // 时间轴
            sb.AppendLine("<div class=\"time-axis\">");
            var tickCount = 5;
            for (var i = 0; i <= tickCount; i++)
            {
                var percent = i * 100.0 / tickCount;
                var time = duration * i / tickCount;
                sb.AppendLine($"<span class=\"time-tick\" style=\"left:{percent}%\">{time:F0}ms</span>");
            }
            sb.AppendLine("</div>");

            // 记录条
            foreach (var record in sortedRecords)
            {
                var left = (record.StartTimestamp - timeStart) * 100.0 / duration;
                var width = Math.Max(0.5, (record.EndTimestamp - record.StartTimestamp) * 100.0 / duration);
                var top = 25 + recordRows[record.RecordId] * 22;

                var typeClass = record.Type.ToString().ToLower();
                if (!record.IsSuccess) typeClass = "failed";

                var name = Runtime.PathUtils.GetFileName(record.DisplayPath);
                sb.AppendLine($"<div class=\"bar {typeClass}\" style=\"left:{left:F2}%;width:{width:F2}%;top:{top}px\" title=\"{EscapeHtml(name)}&#10;Type: {record.Type}&#10;Duration: {record.Duration:F1}ms&#10;Source: {record.Source}\">{EscapeHtml(name)}</div>");
            }

            sb.AppendLine("</div></div>");
        }

        private static string EscapeHtml(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}