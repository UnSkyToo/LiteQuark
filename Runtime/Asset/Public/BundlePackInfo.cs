using System;
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
        
        public BundleInfo GetBundleInfoFromBundlePath(string bundlePath)
        {
            return BundlePathToBundleCache_.TryGetValue(bundlePath, out var info) ? info : null;
        }

        public BundleInfo GetBundleInfoFromAssetPath(string assetPath)
        {
            return AssetPathToBundleCache_.TryGetValue(assetPath, out var info) ? info : null;
        }

        public string ToJson()
        {
            var jsonText = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            return jsonText;
        }

        public static BundlePackInfo FromJson(string jsonText)
        {
            var packInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<BundlePackInfo>(jsonText);
            return packInfo;
        }
    }
}