using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteObjectPoolMgr : Singleton<LiteObjectPoolMgr>
    {
        private readonly Dictionary<string, LiteObjectPool> PoolList_ = new Dictionary<string, LiteObjectPool>();
        private Transform Parent_;

        private LiteObjectPoolMgr()
        {
        }
        
        public void Startup()
        {
            Parent_ = new GameObject("ObjectPool").transform;
            Parent_.localPosition = Vector3.zero;
            Object.DontDestroyOnLoad(Parent_.gameObject);
        }

        public void Shutdown()
        {
            foreach (var pool in PoolList_)
            {
                pool.Value.Dispose();
            }
            PoolList_.Clear();

            if (Parent_ != null)
            {
                Object.DestroyImmediate(Parent_.gameObject);
                Parent_ = null;
            }
        }

        public LiteObjectPool Get(string prefabPth)
        {
            if (!PoolList_.ContainsKey(prefabPth))
            {
                PoolList_.Add(prefabPth, new LiteObjectPool(Parent_, prefabPth));
            }

            return PoolList_[prefabPth];
        }
    }
}