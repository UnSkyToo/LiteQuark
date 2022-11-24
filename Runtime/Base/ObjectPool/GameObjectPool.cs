using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class GameObjectPool : ObjectPoolBase<GameObject>
    {
        private string GoPath_;
        private GameObject Go_;
        
        public GameObjectPool()
        {
        }

        public override void Initialize(object param)
        {
            GoPath_ = (string)param;
            
            LiteRuntime.GetAssetSystem().LoadAsset<GameObject>(GoPath_, (go) =>
            {
                Go_ = go;
            });
        }

        protected override GameObject OnCreate()
        {
            return Object.Instantiate(Go_);
        }

        protected override void OnAlloc(GameObject go)
        {
            go.SetActive(true);
        }

        protected override void OnRecycle(GameObject go)
        {
            go.SetActive(false);
        }

        protected override void OnDelete(GameObject go)
        {
            Object.DestroyImmediate(go);
        }
    }
}