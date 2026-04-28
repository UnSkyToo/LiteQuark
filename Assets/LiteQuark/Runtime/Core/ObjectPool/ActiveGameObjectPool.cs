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
                if (_templateHandle == handle)
                {
                    OnLoadTemplate(handle.Result);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (_templateHandle == handle)
                {
                    _templateHandle = null;
                }

                handle.Dispose();
                LLog.Error("Load pool template error: {0}", ex.Message);
            }
        }

        public override void Dispose()
        {
            if (_templateHandle != null)
            {
                _templateHandle.Dispose();
                _templateHandle = null;
                Template = null;
            }
            
            base.Dispose();
        }

        protected override void OnRelease(GameObject go)
        {
            go.SetActive(false);
            go.transform.SetParent(Parent, false);
        }

        public override void Alloc(Transform parent, System.Action<GameObject> callback)
        {
            base.Alloc(parent, (go) =>
            {
                if (go != null)
                {
                    go.SetActive(true);
                }

                LiteUtils.SafeInvoke(callback, go);
            });
        }
    }
}
