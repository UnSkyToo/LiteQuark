using UnityEditor.IMGUI.Controls;
using UnityEngine;
using LiteQuark.Runtime;

namespace LiteQuark.Editor.LoadProfiler
{
    internal class ProfilerTreeItem : TreeViewItem
    {
        public AssetLoadEventType LoadType { get; set; }
        public AssetLoadEventSource LoadSource { get; set; }
        public string Path { get; set; }
        public float Duration { get; set; }
        public long Size { get; set; }

        public bool IsSuccess { get; set; }
        public long Timestamp { get; set; }
        public long RecordId { get; set; }
        public long SessionId { get; set; }
        public string[] Dependencies { get; set; }
        public Texture TypeIcon { get; set; }

        public ProfilerTreeItem()
        {
            Dependencies = System.Array.Empty<string>();
        }
    }
}
