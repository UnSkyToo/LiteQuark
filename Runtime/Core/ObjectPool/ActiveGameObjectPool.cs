using UnityEngine;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// Use SetActive replace change position for recycle game object
    /// </summary>
    public class ActiveGameObjectPool : BaseGameObjectPool
    {
        public override string Name => PathUtils.GetFileName(Key);

        public ActiveGameObjectPool()
            : base()
        {
        }

        public override void Initialize(string key, params object[] args)
        {
            base.Initialize(key, args);

            LiteRuntime.Asset.LoadAssetAsync<GameObject>(Key, OnLoadTemplate);
        }

        public override void Dispose()
        {
            if (Template != null)
            {
                LiteRuntime.Asset.UnloadAsset(Template);
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

                callback?.Invoke(go);
            });
        }
    }
}