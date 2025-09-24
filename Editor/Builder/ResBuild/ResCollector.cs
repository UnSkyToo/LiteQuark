using System;
using System.Collections.Generic;
using System.Linq;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal class ResCollector
    {
        public AssetBundleManifest Manifest { get; set; }
        
        private readonly string _defaultBundlePath = $"default{LiteConst.BundleFileExt}";
        private readonly Dictionary<string, BundleInfo> _bundleInfoCache = new Dictionary<string, BundleInfo>();
        private ResCollectorSetting _setting = null;
        private VersionPackInfo _packInfo = null;
        private int _bundleID = 1;

        public VersionPackInfo GetVersionPackInfo(ProjectBuilder builder)
        {
            return GetVersionPackInfo(builder.Version, builder.Target, builder.ResConfig.HashMode, builder.ResConfig.FlatMode);
        }
        
        public VersionPackInfo GetVersionPackInfo(string version, BuildTarget target, bool hashMode, bool flatMode)
        {
            if (_packInfo == null)
            {
                _setting = ResCollectorSetting.GetOrCreateSetting();
                _bundleInfoCache.Clear();
                _bundleID = 1;
                CollectBundleInfo(LiteConst.AssetRootPath);
                _packInfo = new VersionPackInfo(version, target.ToString(), hashMode, flatMode, _bundleInfoCache.Values.ToArray());
                
                var checkResult = ResDependencyChecker.FindUniqueCycles(_packInfo);
                if (checkResult.Count > 0)
                {
                    foreach (var cycle in checkResult)
                    {
                        LEditorLog.Error("发现循环依赖: " + string.Join(" -> ", cycle));
                    }
                }
            }
            return _packInfo;
        }

        public void CleanVersionPackInfo()
        {
            _setting = null;
            _packInfo = null;
            Manifest = null;
        }

        private void AddToBundleInfoCache(string bundlePath, string[] assetList, string[] dependencyList)
        {
            if (_bundleInfoCache.TryGetValue(bundlePath, out var cache))
            {
                cache.BundlePath = bundlePath;
                cache.AssetList = ArrayUtils.AppendArray(cache.AssetList, assetList, false);
                cache.DependencyList = ArrayUtils.AppendArray(cache.DependencyList, dependencyList, false);
            }
            else
            {
                cache = new BundleInfo(_bundleID++, bundlePath, assetList, dependencyList);
                _bundleInfoCache.Add(bundlePath, cache);
            }
        }
        
        private void CollectBundleInfo(string rootPath)
        {
            rootPath = PathUtils.UnifyPath(rootPath);

            if (!PathFilter(rootPath))
            {
                return;
            }

            if (!PathUtils.DirectoryExists(rootPath))
            {
                return;
            }

            if (!ValidFilter(rootPath))
            {
                return;
            }
            
            var subBundlePathList = PathUtils.GetDirectoryList(rootPath);
            
            foreach (var subBundlePath in subBundlePathList)
            {
                CollectBundleInfo(subBundlePath);
            }
            
            CreateBundleInfo(rootPath);
        }
        
        private void CreateBundleInfo(string bundleFullPath)
        {
            var filePathList = PathUtils.GetFileList(bundleFullPath);
            CreateBundleInfo(bundleFullPath, filePathList);
        }
        
        private void CreateBundleInfo(string bundleFullPath, string[] filePathList)
        {
            var assetPathList = filePathList.Where(AssetFilter).ToArray();
            
            if (assetPathList.Length > 0)
            {
                var assetList = new List<string>();
                var dependencyList = new List<string>();
                
                foreach (var assetPath in assetPathList)
                {
                    assetList.Add(PathUtils.GetRelativeAssetRootPath(assetPath).ToLower());
                    
                    foreach (var dep in GetDependencyList(assetPath))
                    {
                        if (!dependencyList.Contains(dep))
                        {
                            dependencyList.Add(dep);
                        }
                    }
                }

                var bundlePath = GetBundlePathFromFullPath(bundleFullPath);
                dependencyList.Remove(bundlePath);
                AddToBundleInfoCache(bundlePath, assetList.ToArray(), dependencyList.ToArray());
            }
        }
        
        private string[] GetDependencyList(string assetPath)
        {
            var dependencyPathList = AssetDatabase.GetDependencies(assetPath);
            var result = new List<string>();

            foreach (var dependencyPath in dependencyPathList)
            {
                if (string.Compare(dependencyPath, assetPath, StringComparison.OrdinalIgnoreCase) == 0 || !AssetFilter(dependencyPath))
                {
                    continue;
                }
                
                var path = GetBundlePathFromFullPath(dependencyPath);
                if (!result.Contains(path))
                {
                    if (PathUtils.PathIsFile(dependencyPath))
                    {
                        CreateBundleInfo(dependencyPath, new[] { PathUtils.GetRelativeAssetRootPath(dependencyPath).ToLower() });
                    }
                    result.Add(path);
                }
            }

            return result.ToArray();
        }
        
        private string GetBundlePathFromFullPath(string fullPath)
        {
            var realPath = GetRealBundlePathBySetting(fullPath);
            var bundlePath = PathUtils.GetRelativeAssetRootPath(realPath);

            if (string.IsNullOrWhiteSpace(bundlePath))
            {
                return _defaultBundlePath;
            }

            bundlePath = PathUtils.GetPathFromFullPath(bundlePath);
            bundlePath = bundlePath.ToLower();
            
            return $"{bundlePath}{LiteConst.BundleFileExt}";
        }

        private string GetRealBundlePathBySetting(string bundlePath)
        {
            if (_setting.IgnorePathList.Count == 0)
            {
                return bundlePath;
            }
            
            var lastIndividualPath = bundlePath;
            
            while (!string.IsNullOrEmpty(bundlePath))
            {
                if (_setting.IsIgnorePath(bundlePath))
                {
                    lastIndividualPath = PathUtils.GetParentPath(bundlePath);
                    bundlePath = lastIndividualPath;
                }
                else
                {
                    bundlePath = PathUtils.GetParentPath(bundlePath);
                }
            }

            return lastIndividualPath;
        }
        
        private bool AssetFilter(string filePath)
        {
            if (!PathFilter(filePath))
            {
                return false;
            }

            var ext = System.IO.Path.GetExtension(filePath);
            if (ext is ".meta" or ".dll" or ".cs" or ".js" or ".boo" or ".psd")
            {
                return false;
            }

            // Fix font loss caused by using Text component in old projects.
            // if (ext is ".ttf" or ".ttc" or ".otf")
            // {
            //     return false;
            // }

            return true;
        }

        private bool PathFilter(string filePath)
        {
            if (filePath.StartsWith("Packages"))
            {
                return false;
            }
            
            if (filePath.Contains("#") || filePath.Contains("~"))
            {
                return false;
            }

            return true;
        }

        private bool ValidFilter(string filePath)
        {
            foreach (var ch in LiteConst.InvalidFileNameChars)
            {
                if (filePath.Contains(ch))
                {
                    LEditorLog.Error($"invalid path with '{ch}' : {filePath}");
                    return false;
                }
            }

            return true;
        }
    }
}