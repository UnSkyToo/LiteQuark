using System.IO;
using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;

namespace LiteBattle.Editor
{
    public static class LiteAgentHelper
    {
        public static void DeleteAsset(string fullPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<LiteAgentConfig>(fullPath);
            if (asset != null && !string.IsNullOrWhiteSpace(asset.StateGroup))
            {
                var statePath = PathUtils.ConcatPath(LiteStateUtils.GetTimelineRootPath(), asset.StateGroup);
                if (Directory.Exists(statePath))
                {
                    // Directory.Delete(statePath, true);
                    AssetDatabase.DeleteAsset(statePath);
                }
            }
            AssetUtils.DeleteAsset(fullPath);
        }
    }
}