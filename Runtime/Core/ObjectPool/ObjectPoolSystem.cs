using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class ObjectPoolSystem : ISystem
    {
        private readonly Dictionary<string, IBasePool> _poolCache = new ();
        private Transform _root;

        public ObjectPoolSystem()
        {
            _poolCache.Clear();
        }
        
        public Task<bool> Initialize()
        {
            _root = UnityUtils.CreateHoldGameObject("ObjectPool").transform;
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            foreach (var pool in _poolCache)
            {
                pool.Value.Dispose();
            }
            _poolCache.Clear();

            if (_root != null)
            {
                Object.DestroyImmediate(_root.gameObject);
                _root = null;
            }
        }

        public void RemoveUnusedPools()
        {
            var removeList = new List<IBasePool>();
            foreach (var pool in _poolCache)
            {
                if (pool.Value.CountActive > 0)
                {
                    continue;
                }
                removeList.Add(pool.Value);
            }
            foreach (var pool in removeList)
            {
                pool.Dispose();
                _poolCache.Remove(pool.Key);
            }
        }

        public Transform GetPoolRoot()
        {
            return _root;
        }

        public Dictionary<string, IBasePool> GetPoolCache()
        {
            return _poolCache;
        }

        public TPool GetPool<TPool>(string key, params object[] args) where TPool : class, IBasePool
        {
            if (_poolCache.TryGetValue(key, out var pool))
            {
                return pool as TPool;
            }

            return AddPool<TPool>(key, args);
        }
        
        public TPool AddPool<TPool>(string key, params object[] args) where TPool : IBasePool
        {
            var pool = System.Activator.CreateInstance<TPool>();
            pool.Initialize(key, args);
            _poolCache.Add(key, pool);
            return pool;
        }

        public void RemovePool<TPool>(TPool pool) where TPool : IBasePool
        {
            if (pool == null)
            {
                return;
            }

            if (_poolCache.ContainsKey(pool.Key))
            {
                pool.Dispose();
                _poolCache.Remove(pool.Key);
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
            return GetPool<EmptyGameObjectPool>(key, _root);
        }

        public ActiveGameObjectPool GetActiveGameObjectPool(string key)
        {
            return GetPool<ActiveGameObjectPool>(key, _root);
        }

        public PositionGameObjectPool GetPositionGameObjectPool(string key)
        {
            return GetPool<PositionGameObjectPool>(key, _root);
        }

        public ParticlePool GetParticlePool(string path)
        {
            return GetPool<ParticlePool>(path, _root);
        }
    }
}