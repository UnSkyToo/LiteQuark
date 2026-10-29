using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// Use SetActive replace change position for recycle game object
    /// </summary>
    public class ActiveGameObjectPool : BaseGameObjectPool
    {
        public override string Name => PathUtils.GetFileName(Key);

        private AssetHandle<GameObject> _templateHandle;

        public ActiveGameObjectPool()
            : base()
        {
        }

        public override void Initialize(string key, params object[] args)
        {
            base.Initialize(key, args);

            _templateHandle = LiteRuntime.Asset.LoadAssetHandle<GameObject>(Key);
            LoadTemplateAsync(_templateHandle).Forget();
        }

        private async UniTaskVoid LoadTemplateAsync(AssetHandle<GameObject> handle)
        {
            try
            {
                await handle;
                if (handle.State == AssetHandleState.Failed)
                {
                    LLog.Error("Load pool template failed: {0}, {1}", Key, handle.Error?.Message);
                }
                OnLoadTemplate(handle.Result);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_templateHandle != null)
            {
                var handle = _templateHandle;
                _templateHandle = null;
                handle.Dispose();
                Template = null;
            }
        }

        protected override void OnRelease(GameObject go)
        {
            go.SetActive(false);
            go.transform.SetParent(Parent, false);
        }

        public override async UniTask<GameObject> Alloc(Transform parent)
        {
            var go = await base.Alloc(parent);
            if (go != null)
            {
                go.SetActive(true);
            }
            return go;
        }
    }
}
