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
        public AssetBundleBuild[] Builds { get; set; } = Array.Empty<AssetBundleBuild>();

        private readonly string _defaultBundlePath = $"default{LiteConst.BundleFileExt}";
        private readonly Dictionary<string, BundleInfo> _bundleInfoCache = new();
        private readonly Dictionary<string, string[]> _dependencyCache = new();
        private readonly Dictionary<string, string> _assetSourcePathMap = new(StringComparer.Ordinal);
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
                _dependencyCache.Clear();
                _assetSourcePathMap.Clear();
                _bundleID = 1;
                CollectBundleInfo(LiteConst.AssetRootPath);
                var packInfo = new VersionPackInfo(version, target.ToString(), hashMode, flatMode, _bundleInfoCache.Values.ToArray());

                var cycles = AssetBundleDependencyValidator.FindUniqueCycles(packInfo);
                if (cycles.Count > 0)
                {
                    var message = $"发现循环依赖:{Environment.NewLine}{AssetBundleDependencyValidator.FormatCycles(cycles)}";
                    LEditorLog.Error(message);
                    throw new InvalidOperationException(message);
                }

                _packInfo = packInfo;
            }
            return _packInfo;
        }

        public void CleanVersionPackInfo()
        {
            _bundleInfoCache.Clear();
            _dependencyCache.Clear();
            _assetSourcePathMap.Clear();
            _bundleID = 1;
            _setting = null;
            _packInfo = null;
            Manifest = null;
            Builds = Array.Empty<AssetBundleBuild>();
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
            var assetList = new List<string>();
            var dependencySet = new HashSet<string>();

            foreach (var filePath in filePathList)
            {
                if (!AssetFilter(filePath))
                {
                    continue;
                }

                var sourcePath = AssetPath.NormalizeProjectPath(filePath);

                if (!TryRegisterAssetSourcePath(sourcePath, out var assetKey))
                {
                    continue;
                }

                // assetList.Add(GetManifestAssetPath(assetKey));
                assetList.Add(assetKey);

                foreach (var dependency in GetDependencyList(sourcePath))
                {
                    dependencySet.Add(dependency);
                }
            }

            if (assetList.Count == 0)
            {
                return;
            }

            var bundlePath = GetBundlePathFromFullPath(bundleFullPath);
            dependencySet.Remove(bundlePath);

            AddToBundleInfoCache(bundlePath, assetList.ToArray(), dependencySet.ToArray());
        }

        private string[] GetDependencyList(string assetPath)
        {
            if (!_dependencyCache.TryGetValue(assetPath, out var dependencyPathList))
            {
                dependencyPathList = AssetDatabase.GetDependencies(assetPath);
                _dependencyCache.Add(assetPath, dependencyPathList);
            }

            var result = new HashSet<string>();

            foreach (var dependencyPath in dependencyPathList)
            {
                if (string.Equals(dependencyPath, assetPath, StringComparison.OrdinalIgnoreCase) || !AssetFilter(dependencyPath) || !PathUtils.PathIsFile(dependencyPath))
                {
                    continue;
                }

                var sourcePath = AssetPath.NormalizeProjectPath(dependencyPath);
                // 每个依赖资源都尝试收集，资源去重由登记字典负责。
                CreateBundleInfo(sourcePath, new[] { sourcePath });
                // 这里仅对 Bundle 依赖边去重。
                result.Add(GetBundlePathFromFullPath(sourcePath));
            }

            return result.ToArray();
        }

        private string GetBundlePathFromFullPath(string fullPath)
        {
            var realPath = GetRealBundlePathBySetting(fullPath);

            if (string.Equals(realPath, LiteConst.AssetRootPath, StringComparison.OrdinalIgnoreCase))
            {
                return _defaultBundlePath;
            }

            var bundlePath = AssetPath.TryGetRelativeToRoot(realPath, LiteConst.AssetRootPath, out var relativePath) ? relativePath : realPath;
            bundlePath = PathUtils.GetPathFromFullPath(bundlePath);

            if (string.IsNullOrEmpty(bundlePath))
            {
                return _defaultBundlePath;
            }

            bundlePath = PathUtils.FormatAssetPath(bundlePath);
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

        private bool TryRegisterAssetSourcePath(string sourcePath, out string assetKey)
        {
            assetKey = AssetPath.ResolveProjectAssetPath(sourcePath);

            if (_assetSourcePathMap.TryGetValue(assetKey, out var existingPath))
            {
                if (!string.Equals(existingPath, sourcePath, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Asset path collision: {existingPath} <-> {sourcePath}");
                }

                return false;
            }

            _assetSourcePathMap.Add(assetKey, sourcePath);
            return true;
        }

        private static string GetManifestAssetPath(string assetKey)
        {
            return AssetPath.TryGetRelativeToRoot(assetKey, LiteConst.AssetRootPath, out var relativePath) ? relativePath : assetKey;
        }

        internal string GetAssetSourcePath(string assetPath)
        {
            var assetKey = AssetPath.ResolveProjectAssetPath(assetPath);

            if (_assetSourcePathMap.TryGetValue(assetKey, out var sourcePath))
            {
                return sourcePath;
            }

            throw new InvalidOperationException($"Asset source path was not collected: {assetPath}");
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
