﻿using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    [Serializable]
    public sealed class BundlePackInfo
    {
        public string Platform { get; set; }
        public bool IsValid { get; set; }
        public BundleInfo[] BundleList { get; set; }

        private Dictionary<string, BundleInfo> BundlePathToBundleCache_ = new ();
        private Dictionary<string, BundleInfo> AssetPathToBundleCache_ = new ();

        public BundlePackInfo()
        {
            IsValid = false;
        }

        public BundlePackInfo(string platform, BundleInfo[] bundleList)
        {
            Platform = platform;
            BundleList = bundleList;
            
            IsValid = true;
        }

        public void Initialize()
        {
            BundlePathToBundleCache_.Clear();
            AssetPathToBundleCache_.Clear();
            
            foreach (var bundle in BundleList)
            {
                BundlePathToBundleCache_.Add(bundle.BundlePath, bundle);
                
                foreach (var assetPath in bundle.AssetList)
                {
                    AssetPathToBundleCache_.Add(assetPath, bundle);
                }
            }
        }

        private void SimplifyPath()
        {
            var bundleIDMap = new Dictionary<string, string>();
            foreach (var bundle in BundleList)
            {
                bundleIDMap.Add(bundle.BundlePath, bundle.BundleID.ToString());
            }
            
            foreach (var bundle in BundleList)
            {
                var bundlePath = $"{bundle.BundlePath.Replace(".ab", string.Empty)}/";
                for (var index = 0; index < bundle.AssetList.Length; index++)
                {
                    bundle.AssetList[index] = PathUtils.GetRelativePath(bundlePath, bundle.AssetList[index]);
                }

                for (var index = 0; index < bundle.DependencyList.Length; index++)
                {
                    bundle.DependencyList[index] = bundleIDMap[bundle.DependencyList[index]];
                }
            }
        }

        private void RestorePath()
        {
            var bundleIDMap = new Dictionary<string, string>();
            foreach (var bundle in BundleList)
            {
                bundleIDMap.Add(bundle.BundleID.ToString(), bundle.BundlePath);
            }
            
            foreach (var bundle in BundleList)
            {
                var bundlePath = $"{bundle.BundlePath.Replace(".ab", string.Empty)}/";
                for (var index = 0; index < bundle.AssetList.Length; index++)
                {
                    if (!bundle.AssetList[index].Contains('/'))
                    {
                        bundle.AssetList[index] = $"{bundlePath}{bundle.AssetList[index]}";
                    }
                }

                for (var index = 0; index < bundle.DependencyList.Length; index++)
                {
                    bundle.DependencyList[index] = bundleIDMap[bundle.DependencyList[index]];
                }
            }
        }
        
        public BundleInfo GetBundleInfoFromBundlePath(string bundlePath)
        {
            if (BundlePathToBundleCache_.TryGetValue(bundlePath, out var info))
            {
                return info;
            }

            LLog.Error($"can't get bundle info : {bundlePath}");
            return null;
        }

        public BundleInfo GetBundleInfoFromAssetPath(string assetPath)
        {
            if (AssetPathToBundleCache_.TryGetValue(assetPath, out var info))
            {
                return info;
            }
            
            LLog.Error($"can't get bundle info : {assetPath}");
            return null;
        }

        public string ToJson()
        {
            SimplifyPath();
            // var jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            var jsonText = LitJson.JsonMapper.ToJson(this);
            return jsonText;
        }

        private static BundlePackInfo FromJson(string jsonText)
        {
            // var packInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<BundlePackInfo>(jsonText);
            var packInfo = LitJson.JsonMapper.ToObject<BundlePackInfo>(jsonText);
            packInfo.RestorePath();
            return packInfo;
        }
        
        public static void LoadBundlePack(string bundleUri, Action<BundlePackInfo> callback)
        {
            try
            {
                LiteRuntime.Task.UnityWebGetRequestTask(bundleUri, 0, true, (downloadHandler) =>
                {
                    if (downloadHandler.isDone)
                    {
                        var info = FromJson(downloadHandler.text);

                        if (info is not { IsValid: true })
                        {
                            LLog.Error($"bundle package parse error\n{downloadHandler.error}");
                            callback?.Invoke(null);
                            return;
                        }

                        info.Initialize();
                        callback?.Invoke(info);
                    }
                    else
                    {
                        LLog.Error($"load bundle package error\n{downloadHandler.error}");
                    }
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}