using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class LoadAssetBaseTask : BaseTask
    {
        protected readonly AssetBundle Bundle;
        protected readonly string Name;
        private UnityEngine.Object _asset;
        private Action<UnityEngine.Object> _callback;
        
        protected LoadAssetBaseTask(AssetBundle bundle, string name, Action<UnityEngine.Object> callback)
        {
            Bundle = bundle;
            Name = name;
            _asset = null;
            _callback = callback;
        }
        
        public override void Dispose()
        {
            _callback = null;
        }
        
        protected void OnAssetLoaded(UnityEngine.Object asset)
        {
            _asset = asset;
            
            _callback?.Invoke(_asset);
            Complete(_asset);
        }

        public UnityEngine.Object GetAsset()
        {
            return _asset;
        }
    }
}