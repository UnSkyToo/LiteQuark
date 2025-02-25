using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteObjectPool
    {
        private Transform Parent_;
        private readonly GameObject Prefab_;
        private readonly Stack<GameObject> ObjectList_ = new Stack<GameObject>();

        public LiteObjectPool(Transform parent, string prefabPath, int count = 2)
        {
            Parent_ = new GameObject(Path.GetFileNameWithoutExtension(prefabPath)).transform;
            Parent_.SetParent(parent, false);
            Parent_.localPosition = Vector3.zero;
            
            Prefab_ = LiteAssetMgr.Instance.LoadAsset<GameObject>(prefabPath);
            for (var i = 0; i < count; ++i)
            {
                CreateInstance();
            }
        }

        public void Dispose()
        {
            foreach (var obj in ObjectList_)
            {
                Object.Destroy(obj);
            }
            ObjectList_.Clear();

            if (Parent_ != null)
            {
                Object.DestroyImmediate(Parent_.gameObject);
                Parent_ = null;
            }
        }

        public GameObject Alloc()
        {
            if (ObjectList_.Count == 0)
            {
                CreateInstance();
            }

            var go = ObjectList_.Pop();
            SetActive(go, true);
            return go;
        }

        public void Recycle(GameObject go)
        {
            SetActive(go, false);
            ObjectList_.Push(go);
        }

        private GameObject CreateInstance()
        {
            var go = Object.Instantiate(Prefab_);
            go.transform.SetParent(Parent_, false);
            ObjectList_.Push(go);
            SetActive(go, false);
            return go;
        }

        private void SetActive(GameObject go, bool active)
        {
            if (active)
            {
                go.transform.SetParent(null, false);
            }
            else
            {
                go.transform.SetParent(Parent_, false);
            }
            
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.SetActive(active);
        }
    }
}