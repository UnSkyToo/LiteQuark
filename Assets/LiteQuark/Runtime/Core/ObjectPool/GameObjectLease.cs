using UnityEngine;

namespace LiteQuark.Runtime
{
    /// <summary>Owns one borrowed instance until it is returned to its pool.</summary>
    public sealed class GameObjectLease : IDispose
    {
        private BaseGameObjectPool _pool;

        public GameObject Value { get; private set; }

        internal GameObjectLease(BaseGameObjectPool pool, GameObject value)
        {
            _pool = pool;
            Value = value;
        }

        public void Dispose()
        {
            var pool = _pool;
            if (pool == null)
            {
                return;
            }

            var value = Value;
            // Returning can synchronously invoke Unity callbacks that dispose this lease again.
            _pool = null;
            Value = null;
            pool.Release(value);
        }
    }
}
