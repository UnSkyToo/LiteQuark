using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public sealed class ObjectPoolSystem : ISystem
    {
        private readonly Dictionary<object, IObjectPool> PoolCache_ = new ();

        public ObjectPoolSystem()
        {
            PoolCache_.Clear();
        }

        public void Dispose()
        {
            foreach (var current in PoolCache_)
            {
                current.Value.Clean();
            }
            PoolCache_.Clear();
        }

        public TPool GetPool<TPool>(object key) where TPool : IObjectPool
        {
            if (PoolCache_.TryGetValue(key, out var pool) && pool is TPool tpool)
            {
                return tpool;
            }

            return AddPool<TPool>(key);
        }

        public TPool AddPool<TPool>(object key) where TPool : IObjectPool
        {
            var pool = Activator.CreateInstance<TPool>();
            pool.Initialize(key);
            PoolCache_.Add(key, pool);
            return pool;
        }
    }
}