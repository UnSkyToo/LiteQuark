using System.Collections.Generic;
using System.IO;
using LiteBattle.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteGame
{
    public sealed class ResourceHandler : ILiteAssetHandler
    {
        private readonly string RootPath_ = $"{Application.dataPath}/StandaloneAssets/demo/";
        
        public List<string> GetFileList(string path, string ext)
        {
            var results = new List<string>();
            var fileList = Directory.GetFiles($"{RootPath_}{path}");
            
            foreach (var file in fileList)
            {
                if (file.EndsWith(ext))
                {
                    results.Add(file.Replace(RootPath_, string.Empty));
                }
            }

            return results;
        }

        public List<string> GetDirectoryList(string path)
        {
            var results = new List<string>();
            var directoryList = Directory.GetDirectories($"{RootPath_}{path}");

            foreach (var directory in directoryList)
            {
                results.Add(directory.Replace(RootPath_, string.Empty));
            }

            return results;
        }

        public T LoadAsset<T>(string path) where T : Object
        {
            var resourcePath = $"Assets/StandaloneAssets/demo/{path}";
            return AssetDatabase.LoadAssetAtPath<T>(resourcePath);
        }

        public void UnloadAsset<T>(T asset) where T : Object
        {
        }
    }
}