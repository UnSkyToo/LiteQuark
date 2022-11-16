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
        private List<string> RealPathList_;

        public NavigationData()
        {
            PathList = Array.Empty<string>();
            RealPathList_ = new List<string>();
        }

        public string[] GetPathList()
        {
            return RealPathList_.ToArray();
        }

        public void AddPath(string path)
        {
            if (RealPathList_.Contains(path))
            {
                return;
            }
            
            RealPathList_.Add(path);
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
            RealPathList_.Remove(path);
        }

        private void FlushWhenLoad()
        {
            RealPathList_.AddRange(PathList);
        }

        private void FlushWhenSave()
        {
            PathList = RealPathList_.ToArray();
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
            PathHelper.CreateDirectory(jsonPath);
            File.WriteAllText(jsonPath, jsonText);
        }
    }
}