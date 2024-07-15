using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace LiteQuark.Editor
{
    internal static class AssetViewerUtils
    {
        public static string GetSizeString(long size)
        {
            if (size < 1024)
            {
                return $"{size:0.0} B";
            }
            
            if (size < 1024 * 1024)
            {
                return $"{size / 1024.0f:0.0} KB";
            }
            
            if (size < 1024 * 1024 * 1024)
            {
                return $"{size / 1024.0f / 1024.0f:0.00} MB";
            }
            
            return $"{size / 1024.0f / 1024.0f / 1024.0f:0.00} GB";
        }

        public static void SortTreeItemList(List<TreeViewItem> items, int type)
        {
            if (type == 0)
            {
                return;
            }
            
            if (type < 0)
            {
                items.Sort((a, b) => ((AssetViewerTreeItem)b).Size.CompareTo(((AssetViewerTreeItem)a).Size));
            }
            else
            {
                items.Sort((a, b) => ((AssetViewerTreeItem)a).Size.CompareTo(((AssetViewerTreeItem)b).Size));
            }
        }
    }
}