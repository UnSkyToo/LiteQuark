using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [LiteHideType]
    public sealed class ObjectPoolSystem : ISystem
    {
        private readonly Dictionary<string, IBasePool> _poolCache = new ();
        private Transform _root = null;
        private bool _isClosing;

        public ObjectPoolSystem()
        {
            _poolCache.Clear();
        }

        public UniTask<bool> Initialize()
        {
            _root = UnityUtils.CreateHoldGameObject("ObjectPool").transform;
            return UniTask.FromResult(true);
        }

        public UniTask DisposeAsync()
        {
            _isClosing = true;
            var pools = new List<IBasePool>(_poolCache.Values);
            _poolCache.Clear();
            foreach (var pool in pools)
            {
                pool.Dispose();
            }

            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
                _root = null;
            }

            return UniTask.CompletedTask;
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
                // Earlier cancellation callbacks may have borrowed from this candidate.
                if (pool.CountActive > 0)
                {
                    continue;
                }
                RemovePool(pool);
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
            ThrowIfClosing();
            if (_poolCache.TryGetValue(key, out var pool))
            {
                return pool as TPool;
            }

            return AddPool<TPool>(key, args);
        }

        public TPool AddPool<TPool>(string key, params object[] args) where TPool : IBasePool
        {
            ThrowIfClosing();
            var pool = System.Activator.CreateInstance<TPool>();
            try
            {
                pool.Initialize(key, args);
                _poolCache.Add(key, pool);
            }
            catch
            {
                pool.Dispose();
                throw;
            }
            return pool;
        }

        private void ThrowIfClosing()
        {
            if (_isClosing)
            {
                throw new System.ObjectDisposedException(nameof(ObjectPoolSystem));
            }
        }

        public void RemovePool<TPool>(TPool pool) where TPool : IBasePool
        {
            if (pool == null)
            {
                return;
            }

            if (_poolCache.TryGetValue(pool.Key, out var current) && ReferenceEquals(current, pool))
            {
                // Cancellation may synchronously resume callers that create another pool with this key.
                _poolCache.Remove(pool.Key);
                pool.Dispose();
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

        public UniTask<bool> PreloadPoolAsync<TPool>(string key, params object[] args) where TPool : class, IBasePool
        {
            var pool = GetPool<TPool>(key, args);
            return pool.WaitReadyAsync();
        }

        public async UniTask<bool> PreloadPoolAsync<TPool>(string key, int generateCount, params object[] args) where TPool : class, IBasePool
        {
            var pool = GetPool<TPool>(key, args);
            return generateCount > 0 ? await pool.GenerateAsync(generateCount) : await pool.WaitReadyAsync();
        }
    }
}
