using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    public static class LiteAssetHelper
    {
        public static List<string> GetAssetPathList(string assetType, string rootPath)
        {
            var results = new List<string>();

            if (!Directory.Exists(rootPath))
            {
                return results;
            }
            
            var assetGUIDs = AssetDatabase.FindAssets($"t:{assetType}", new[] {rootPath});
            foreach (var guid in assetGUIDs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                results.Add(path);
            }
            return results;
        }
        
        public static T CreateAsset<T>(string rootPath, string name) where T : ScriptableObject
        {
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }
            
            var fullPath = PathUtils.ConcatPath(rootPath, name); 
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, fullPath);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }
        
        public static void DeleteAsset(string fullPath)
        {
            AssetDatabase.DeleteAsset(fullPath);
            AssetDatabase.SaveAssets();
        }
        
        public static void DuplicateAsset(string fullPath)
        {
            var newFileName = $"{Path.GetFileNameWithoutExtension(fullPath)}_{DateTime.Now.Second}_{DateTime.Now.Millisecond}";
            var newPath = PathUtils.ConcatPath(Path.GetDirectoryName(fullPath), $"{newFileName}{Path.GetExtension(fullPath)}");
            AssetDatabase.CopyAsset(fullPath, newPath);
            AssetDatabase.SaveAssets();
        }
        
        public static void RenameAsset(string fullPath, string newName)
        {
            AssetDatabase.RenameAsset(fullPath, newName);
            AssetDatabase.SaveAssets();
        }

        public static string RandomAssetName(string prefix, int len = 6)
        {
            var result = new StringBuilder();
            result.Append(prefix);
            
            len = Mathf.Max(len, 1);
            for (var i = 0; i < len; ++i)
            {
                var index = UnityEngine.Random.Range(0, 26);
                result.Append((char)((int)'a' + index));
            }

            return result.ToString();
        }
    }
}