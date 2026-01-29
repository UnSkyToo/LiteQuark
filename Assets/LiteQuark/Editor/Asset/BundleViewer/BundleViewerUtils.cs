using System.Collections.Generic;
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
    }
}