using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal class BundleViewerTreeItem : TreeViewItem
    {
        public Texture TypeIcon { get; set; }
        public string Type { get; set; }
        public long Size { get; set; }
        public string Path { get; set; }
        public string[] DependencyList { get; set; }
    }
}