using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class ConfigToolWindow : EditorWindow
    {
        private ExportCache _cache;
        private Vector2 _scrollPos;
        private Vector2 _logScrollPos;
        private Vector2 _fileListScrollPos;
        private readonly List<string> _logs = new List<string>();
        private readonly List<ExcelFileInfo> _excelFiles = new List<ExcelFileInfo>();
        private bool _isExporting;

        [MenuItem("Lite/Config Tool")]
        private static void ShowWin()
        {
            var win = GetWindow<ConfigToolWindow>("Config Tool");
            win.minSize = new Vector2(800, 600);
            win.Show();
        }

        private void OnEnable()
        {
            _cache = new ExportCache();
            RefreshExcelFileList();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawSettings();
            EditorGUILayout.Space(10);
            DrawExcelFileList();
            EditorGUILayout.Space(10);
            DrawActions();
            EditorGUILayout.Space(10);
            DrawLog();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                using (new EditorGUILayout.HorizontalScope())
                {
                    ConfigToolCache.ExcelFolder = EditorGUILayout.TextField("Excel Folder", ConfigToolCache.ExcelFolder);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        var path = EditorUtility.OpenFolderPanel("Select Excel Folder", ConfigToolCache.ExcelFolder, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            ConfigToolCache.ExcelFolder = path;
                            GUI.changed = true;
                        }
                    }
                }
                if (EditorGUI.EndChangeCheck())
                {
                    RefreshExcelFileList();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    ConfigToolCache.JsonOutputFolder = EditorGUILayout.TextField("JSON Output", ConfigToolCache.JsonOutputFolder);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        var path = EditorUtility.OpenFolderPanel("Select JSON Output Folder", ConfigToolCache.JsonOutputFolder, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            ConfigToolCache.JsonOutputFolder = FileUtil.GetProjectRelativePath(path);
                            if (string.IsNullOrEmpty(ConfigToolCache.JsonOutputFolder))
                            {
                                ConfigToolCache.JsonOutputFolder = path;
                            }
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    ConfigToolCache.CodeOutputFolder = EditorGUILayout.TextField("Code Output", ConfigToolCache.CodeOutputFolder);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        var path = EditorUtility.OpenFolderPanel("Select Code Output Folder", ConfigToolCache.CodeOutputFolder, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            ConfigToolCache.CodeOutputFolder = FileUtil.GetProjectRelativePath(path);
                            if (string.IsNullOrEmpty(ConfigToolCache.CodeOutputFolder))
                            {
                                ConfigToolCache.CodeOutputFolder = path;
                            }
                        }
                    }
                }

                ConfigToolCache.CodeNamespace = EditorGUILayout.TextField("Code Namespace", ConfigToolCache.CodeNamespace);
                ConfigToolCache.JsonBasePath = EditorGUILayout.TextField("JSON Base Path", ConfigToolCache.JsonBasePath);
            }
        }

        private void DrawExcelFileList()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Excel Files ({GetSelectedCount()}/{_excelFiles.Count})", EditorStyles.boldLabel);

                if (GUILayout.Button("Select All", GUILayout.Width(70)))
                {
                    SetAllSelected(true);
                }
                if (GUILayout.Button("Deselect All", GUILayout.Width(80)))
                {
                    SetAllSelected(false);
                }
                if (GUILayout.Button("Invert", GUILayout.Width(50)))
                {
                    InvertSelection();
                }
                if (GUILayout.Button("Select Changed", GUILayout.Width(100)))
                {
                    SelectChanged();
                }
                if (GUILayout.Button("Refresh", GUILayout.Width(60)))
                {
                    RefreshExcelFileList();
                }
            }

            if (_excelFiles.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    string.IsNullOrEmpty(ConfigToolCache.ExcelFolder)
                        ? "Please set the Excel Folder path above."
                        : "No .xlsx / .xls files found in the specified folder.",
                    MessageType.Info);
                return;
            }

            _fileListScrollPos = EditorGUILayout.BeginScrollView(_fileListScrollPos, EditorStyles.helpBox, GUILayout.MinHeight(120), GUILayout.MaxHeight(250));

            foreach (var file in _excelFiles)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Checkbox
                    file.Selected = EditorGUILayout.Toggle(file.Selected, GUILayout.Width(18));

                    // Status icon
                    var statusColor = file.IsChanged ? Color.yellow : Color.green;
                    var prevColor = GUI.contentColor;
                    GUI.contentColor = statusColor;
                    EditorGUILayout.LabelField(file.IsChanged ? "*" : "=", GUILayout.Width(14));
                    GUI.contentColor = prevColor;

                    // File name
                    EditorGUILayout.LabelField(file.FileName, GUILayout.MinWidth(150));

                    // Sheet info
                    EditorGUILayout.LabelField(file.SheetInfo, EditorStyles.miniLabel, GUILayout.Width(200));

                    // Size
                    EditorGUILayout.LabelField(file.FileSize, EditorStyles.miniLabel, GUILayout.Width(70));
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("* Changed   = Unchanged", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            var selectedCount = GetSelectedCount();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !_isExporting && selectedCount > 0;

                if (GUILayout.Button($"Export Selected ({selectedCount})", GUILayout.Height(30)))
                {
                    ExportSelected(false);
                }

                if (GUILayout.Button($"Force Export Selected ({selectedCount})", GUILayout.Height(30)))
                {
                    ExportSelected(true);
                }

                GUI.enabled = true;

                if (GUILayout.Button("Clean", GUILayout.Height(30)))
                {
                    Clean();
                }
            }
        }

        private void DrawLog()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    _logs.Clear();
                }
            }

            _logScrollPos = EditorGUILayout.BeginScrollView(_logScrollPos, EditorStyles.helpBox, GUILayout.MinHeight(150));
            foreach (var log in _logs)
            {
                EditorGUILayout.LabelField(log, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.EndScrollView();
        }

        #region Selection Helpers

        private int GetSelectedCount()
        {
            int count = 0;
            foreach (var file in _excelFiles)
            {
                if (file.Selected) count++;
            }
            return count;
        }

        private void SetAllSelected(bool selected)
        {
            foreach (var file in _excelFiles)
            {
                file.Selected = selected;
            }
        }

        private void InvertSelection()
        {
            foreach (var file in _excelFiles)
            {
                file.Selected = !file.Selected;
            }
        }

        private void SelectChanged()
        {
            foreach (var file in _excelFiles)
            {
                file.Selected = file.IsChanged;
            }
        }

        #endregion

        private void RefreshExcelFileList()
        {
            _excelFiles.Clear();

            var excelFolder = ConfigToolCache.ExcelFolder;
            if (string.IsNullOrEmpty(excelFolder) || !Directory.Exists(excelFolder))
            {
                return;
            }

            var files = new List<string>();
            files.AddRange(Directory.GetFiles(excelFolder, "*.xlsx", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(excelFolder, "*.xls", SearchOption.AllDirectories));
            files.RemoveAll(f => Path.GetFileName(f).StartsWith("~$"));
            files.Sort();

            foreach (var filePath in files)
            {
                var info = new ExcelFileInfo
                {
                    FullPath = filePath,
                    FileName = Path.GetFileName(filePath),
                    IsChanged = _cache.IsFileChanged(filePath),
                    Selected = true,
                };

                try
                {
                    var fi = new FileInfo(filePath);
                    info.FileSize = FormatFileSize(fi.Length);
                }
                catch
                {
                    info.FileSize = "?";
                }

                try
                {
                    var dataSet = ExcelReader.ReadExcel(filePath);
                    var sheetNames = new List<string>();
                    foreach (DataTable table in dataSet.Tables)
                    {
                        if (!table.TableName.StartsWith("#"))
                        {
                            sheetNames.Add(table.TableName);
                        }
                    }
                    info.SheetInfo = $"{sheetNames.Count} sheet(s): {string.Join(", ", sheetNames)}";
                }
                catch
                {
                    info.SheetInfo = "(failed to read)";
                }

                _excelFiles.Add(info);
            }
        }

        private void ExportSelected(bool forceAll)
        {
            _isExporting = true;
            _logs.Clear();

            try
            {
                // Collect selected files
                var selectedFiles = new List<ExcelFileInfo>();
                foreach (var file in _excelFiles)
                {
                    if (file.Selected) selectedFiles.Add(file);
                }

                if (selectedFiles.Count == 0)
                {
                    Log("No files selected.");
                    return;
                }

                if (forceAll)
                {
                    foreach (var file in selectedFiles)
                    {
                        _cache.RemoveHash(file.FullPath);
                    }
                }

                Log($"Exporting {selectedFiles.Count} file(s)...");

                // Registration info needs ALL files (selected + unselected) to generate complete ConfigSystemGenerated.cs
                var allRegistrations = new List<TableRegistrationInfo>();
                var anyChanged = false;

                // Process selected files for export
                var selectedPaths = new HashSet<string>();
                foreach (var file in selectedFiles)
                {
                    selectedPaths.Add(file.FullPath);
                }

                foreach (var file in _excelFiles)
                {
                    if (selectedPaths.Contains(file.FullPath))
                    {
                        var changed = forceAll || _cache.IsFileChanged(file.FullPath);
                        if (!changed)
                        {
                            CollectRegistrationInfo(file.FullPath, allRegistrations);
                            Log($"[Skip] {file.FileName} (unchanged)");
                            continue;
                        }

                        anyChanged = true;
                        Log($"[Export] {file.FileName}");

                        try
                        {
                            ExportExcelFile(file.FullPath, allRegistrations);
                            _cache.UpdateHash(file.FullPath);
                        }
                        catch (Exception ex)
                        {
                            Log($"  ERROR: {ex.Message}");
                            Debug.LogException(ex);
                        }
                    }
                    else
                    {
                        // Unselected files still contribute registration info
                        CollectRegistrationInfo(file.FullPath, allRegistrations);
                    }
                }

                if (allRegistrations.Count > 0)
                {
                    var regPath = Path.Combine(ConfigToolCache.CodeOutputFolder, "ConfigSystemGenerated.cs");
                    CodeGenerator.GenerateRegistration(allRegistrations, regPath, ConfigToolCache.CodeNamespace, ConfigToolCache.JsonBasePath);
                    Log($"Generated: ConfigSystemGenerated.cs ({allRegistrations.Count} table(s))");
                }

                _cache.Save();
                RefreshExcelFileList();

                if (anyChanged)
                {
                    AssetDatabase.Refresh();
                }

                Log("Export completed.");
            }
            catch (Exception ex)
            {
                Log($"Export failed: {ex.Message}");
                Debug.LogException(ex);
            }
            finally
            {
                _isExporting = false;
            }
        }

        private void ExportExcelFile(string excelFile, List<TableRegistrationInfo> allRegistrations)
        {
            var dataSet = ExcelReader.ReadExcel(excelFile);

            foreach (DataTable table in dataSet.Tables)
            {
                if (table.TableName.StartsWith("#"))
                {
                    continue;
                }

                if (table.Rows.Count < 3)
                {
                    Log($"  [Skip] Sheet '{table.TableName}' (too few rows)");
                    continue;
                }

                Log($"  Processing sheet: {table.TableName}");

                var (schema, rows) = SheetParser.Parse(table, excelFile);

                var errors = Validator.Validate(schema, rows);
                if (errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        Log($"    Validation Error: {error}");
                    }
                    Log($"  [Failed] Sheet '{table.TableName}' has {errors.Count} validation error(s).");
                    continue;
                }

                var jsonFileName = $"{schema.Name.ToLowerInvariant()}.json";
                var jsonPath = Path.Combine(ConfigToolCache.JsonOutputFolder, jsonFileName);
                JsonExporter.Export(schema, rows, jsonPath);
                Log($"    -> {jsonPath}");

                var dataPath = Path.Combine(ConfigToolCache.CodeOutputFolder, "Data", $"{schema.PascalName}Data.cs");
                CodeGenerator.GenerateDataClass(schema, dataPath, ConfigToolCache.CodeNamespace);
                Log($"    -> {dataPath}");

                allRegistrations.Add(new TableRegistrationInfo
                {
                    DataClassName = schema.PascalName + "Data",
                    TableClassName = schema.PascalName + "Table",
                    JsonFileName = jsonFileName,
                });
            }
        }

        private void CollectRegistrationInfo(string excelFile, List<TableRegistrationInfo> allRegistrations)
        {
            try
            {
                var dataSet = ExcelReader.ReadExcel(excelFile);

                foreach (DataTable table in dataSet.Tables)
                {
                    if (table.TableName.StartsWith("#")) continue;
                    if (table.Rows.Count < 3) continue;

                    var tableName = table.TableName;
                    var pascalName = NameUtils.ToPascalCase(tableName);

                    allRegistrations.Add(new TableRegistrationInfo
                    {
                        DataClassName = pascalName + "Data",
                        TableClassName = pascalName + "Table",
                        JsonFileName = $"{tableName.ToLowerInvariant()}.json",
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"  Warning: failed to read {Path.GetFileName(excelFile)} for registration info: {ex.Message}");
            }
        }

        private void Clean()
        {
            _logs.Clear();

            _cache.Clear();
            _cache = new ExportCache();

            if (Directory.Exists(ConfigToolCache.JsonOutputFolder))
            {
                var jsonFiles = Directory.GetFiles(ConfigToolCache.JsonOutputFolder, "*.json");
                foreach (var file in jsonFiles)
                {
                    File.Delete(file);
                    Log($"Deleted: {file}");
                }
            }

            var dataDir = Path.Combine(ConfigToolCache.CodeOutputFolder, "Data");
            if (Directory.Exists(dataDir))
            {
                Directory.Delete(dataDir, true);
                Log($"Deleted: {dataDir}");
            }

            var regFile = Path.Combine(ConfigToolCache.CodeOutputFolder, "ConfigSystemGenerated.cs");
            if (File.Exists(regFile))
            {
                File.Delete(regFile);
                Log($"Deleted: {regFile}");
            }

            RefreshExcelFileList();
            AssetDatabase.Refresh();
            Log("Clean completed.");
        }

        private void Log(string message)
        {
            _logs.Add(message);
            Repaint();
        }

        private static string FormatFileSize(long bytes) 
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (1024f * 1024f):F1} MB";
        }

        private class ExcelFileInfo
        {
            public string FullPath;
            public string FileName;
            public string SheetInfo;
            public string FileSize;
            public bool IsChanged;
            public bool Selected;
        }
    }
}