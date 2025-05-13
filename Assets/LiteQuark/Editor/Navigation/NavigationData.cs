using System;
using System.Collections.Generic;
using System.IO;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [Serializable]
    public class NavigationData
    {
        [SerializeField]
        private string[] PathList;

        [NonSerialized]
        private List<string> _realPathList;

        public NavigationData()
        {
            PathList = Array.Empty<string>();
            _realPathList = new List<string>();
        }

        public string[] GetPathList()
        {
            return _realPathList.ToArray();
        }

        public void AddPath(string path)
        {
            if (_realPathList.Contains(path))
            {
                return;
            }
            
            _realPathList.Add(path);
        }

        public void AddPath(string[] pathList)
        {
            foreach (var path in pathList)
            {
                AddPath(path);
            }
        }

        public void RemovePath(string path)
        {
            _realPathList.Remove(path);
        }

        private void FlushWhenLoad()
        {
            _realPathList.AddRange(PathList);
        }

        private void FlushWhenSave()
        {
            PathList = _realPathList.ToArray();
        }

        public static NavigationData FromJson(string jsonPath)
        {
            var data = new NavigationData();

            if (File.Exists(jsonPath))
            {
                var jsonText = File.ReadAllText(jsonPath);
                if (!string.IsNullOrWhiteSpace(jsonText))
                {
                    EditorJsonUtility.FromJsonOverwrite(jsonText, data);
                }
            }

            data.FlushWhenLoad();
            return data;
        }

        public static void ToJson(NavigationData data, string jsonPath)
        {
            if (data == null)
            {
                return;
            }
            
            data.FlushWhenSave();
            var jsonText = EditorJsonUtility.ToJson(data, true);
            PathUtils.CreateDirectory(jsonPath);
            File.WriteAllText(jsonPath, jsonText);
        }
    }
}