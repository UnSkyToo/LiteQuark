using System;
using System.Collections.Generic;
using System.IO;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace LiteQuark.Editor
{
    internal static class BundleViewerUtils
    {
        public static void SortTreeItemList(List<TreeViewItem> items, int type)
        {
            if (type == 0)
            {
                return;
            }
            
            if (type < 0)
            {
                items.Sort((a, b) => ((BundleViewerTreeItem)b).Size.CompareTo(((BundleViewerTreeItem)a).Size));
            }
            else
            {
                items.Sort((a, b) => ((BundleViewerTreeItem)a).Size.CompareTo(((BundleViewerTreeItem)b).Size));
            }
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