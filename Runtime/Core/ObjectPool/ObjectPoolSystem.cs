using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class ObjectPoolSystem : ISystem
    {
        private readonly Dictionary<string, IBasePool> PoolCache_ = new ();
        private Transform Root_;

        public ObjectPoolSystem()
        {
            Root_ = UnityUtils.CreateHoldGameObject("ObjectPool").transform;
            PoolCache_.Clear();
        }
        
        public void Initialize(System.Action<bool> callback)
        {
            callback?.Invoke(true);
        }

        public void Dispose()
        {
            foreach (var pool in PoolCache_)
            {
                pool.Value.Dispose();
            }
            PoolCache_.Clear();

            if (Root_ != null)
            {
                Object.DestroyImmediate(Root_.gameObject);
                Root_ = null;
            }
        }

        public Transform GetPoolRoot()
        {
            return Root_;
        }

        public Dictionary<string, IBasePool> GetPoolCache()
        {
            return PoolCache_;
        }

        public TPool GetPool<TPool>(string key, params object[] args) where TPool : class, IBasePool
        {
            if (PoolCache_.TryGetValue(key, out var pool))
            {
                return pool as TPool;
            }

            return AddPool<TPool>(key, args);
        }
        
        public TPool AddPool<TPool>(string key, params object[] args) where TPool : IBasePool
        {
            var pool = System.Activator.CreateInstance<TPool>();
            pool.Initialize(key, args);
            PoolCache_.Add(key, pool);
            return pool;
        }

        public void RemovePool<TPool>(TPool pool) where TPool : IBasePool
        {
            if (pool == null)
            {
                return;
            }

            if (PoolCache_.ContainsKey(pool.Key))
            {
                pool.Dispose();
                PoolCache_.Remove(pool.Key);
            }
        }

        public TCollection AllocCollection<TCollection, TItem>() where TCollection : class, ICollection<TItem>, new()
        {
            return UnityEngine.Pool.CollectionPool<TCollection, TItem>.Get();
        }

        public void RecycleCollection<TCollection, TItem>(TCollection value) where TCollection : class, ICollection<TItem>, new()
        {
            UnityEngine.Pool.CollectionPool<TCollection, TItem>.Release(value);
        }

        public List<TItem> AllocList<TItem>()
        {
            return AllocCollection<List<TItem>, TItem>();
        }

        public void RecycleList<TItem>(List<TItem> list)
        {
            RecycleCollection<List<TItem>, TItem>(list);
        }
        
        public Dictionary<TKey, TValue> AllocDictionary<TKey, TValue>()
        {
            return AllocCollection<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>();
        }

        public void RecycleDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            RecycleCollection<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>>(dictionary);
        }
        
        public HashSet<TItem> AllocHashSet<TItem>()
        {
            return AllocCollection<HashSet<TItem>, TItem>();
        }

        public void RecycleHashSet<TItem>(HashSet<TItem> hash)
        {
            RecycleCollection<HashSet<TItem>, TItem>(hash);
        }

        public EmptyGameObjectPool GetEmptyGameObjectPool(string key)
        {
            return GetPool<EmptyGameObjectPool>(key, Root_);
        }

        public ActiveGameObjectPool GetActiveGameObjectPool(string key)
        {
            return GetPool<ActiveGameObjectPool>(key, Root_);
        }

        public PositionGameObjectPool GetPositionGameObjectPool(string key)
        {
            return GetPool<PositionGameObjectPool>(key, Root_);
        }

        public ParticlePool GetParticlePool(string path)
        {
            return GetPool<ParticlePool>(path, Root_);
        }
    }
}