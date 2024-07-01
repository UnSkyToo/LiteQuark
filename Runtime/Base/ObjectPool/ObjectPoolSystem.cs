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

        public GameObjectPool GetGameObjectPool(string path)
        {
            return GetPool<GameObjectPool>(path, Root_);
        }

        public EmptyGameObjectPool GetEmptyGameObjectPool(string key)
        {
            return GetPool<EmptyGameObjectPool>(key, Root_);
        }

        public ParticlePool GetParticlePool(string path)
        {
            return GetPool<ParticlePool>(path, Root_);
        }
    }
}