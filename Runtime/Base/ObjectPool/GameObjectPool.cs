using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class GameObjectPool : ObjectPoolBase<GameObject>
    {
        private static readonly Vector3 InvalidPosition = new Vector3(-10000, -10000, -10000);
        
        private string GoPath_;
        private Transform Parent_;
        private GameObject Go_;
        
        public GameObjectPool()
        {
        }

        public override void Initialize(object param)
        {
            GoPath_ = (string)param;
            Parent_ = new GameObject(PathUtils.GetFileName(GoPath_)).transform;
            
            LiteRuntime.Get<AssetSystem>().LoadAssetAsync<GameObject>(GoPath_, (go) =>
            {
                Go_ = go;
            });
        }

        public override void Clean()
        {
            base.Clean();
            
            GameObject.DestroyImmediate(Parent_.gameObject);
            Parent_ = null;
        }

        protected override GameObject OnCreate()
        {
            var go = Object.Instantiate(Go_);
            go.transform.SetParent(Parent_, false);
            go.transform.localPosition = InvalidPosition;
            return go;
        }

        protected override void OnAlloc(GameObject go)
        {
            go.transform.localPosition = Vector3.zero;
        }

        protected override void OnRecycle(GameObject go)
        {
            go.transform.localPosition = InvalidPosition;
        }

        protected override void OnDelete(GameObject go)
        {
            Object.DestroyImmediate(go);
        }
    }
}