using System.IO;
using LiteBattle.Runtime;
using LiteQuark.Editor;
using LiteQuark.Runtime;
using UnityEditor;

namespace LiteBattle.Editor
{
    public static class LiteUnitHelper
    {
        public static void DeleteAsset(string fullPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<LiteUnitConfig>(fullPath);
            if (asset != null && !string.IsNullOrWhiteSpace(asset.StateGroup))
            {
                var statePath = PathUtils.ConcatPath(LiteNexusConfig.Instance.GetTimelineDatabasePath(), asset.StateGroup);
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