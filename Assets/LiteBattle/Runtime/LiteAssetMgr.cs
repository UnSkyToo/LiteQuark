using System.Collections.Generic;
using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    public interface ILiteAssetHandler
    {
        List<string> GetFileList(string path, string ext);
        List<string> GetDirectoryList(string path);

        T LoadAsset<T>(string path) where T : UnityEngine.Object;
        void UnloadAsset<T>(T asset) where T : UnityEngine.Object;
    }
    
    public sealed class LiteAssetMgr : Singleton<LiteAssetMgr>
    {
        private ILiteAssetHandler Handler_;

        private LiteAssetMgr()
        {
        }
        
        public void SetHandler(ILiteAssetHandler handler)
        {
            Handler_ = handler;
        }

        public List<string> GetFileList(string path, string ext)
        {
            return Handler_.GetFileList(path, ext);
        }

        public List<string> GetDirectoryList(string path)
        {
            return Handler_.GetDirectoryList(path);
        }

        public T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            return Handler_.LoadAsset<T>(path);
        }

        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object
        {
            Handler_.UnloadAsset(asset);
        }
    }
}