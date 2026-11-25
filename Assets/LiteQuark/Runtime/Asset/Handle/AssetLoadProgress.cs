using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    internal struct AssetLoadProgress
    {
        private Func<float> _read;
        private float _value;

        public AssetLoadProgress(Func<float> read)
        {
            _read = read;
            _value = 0f;
        }

        public float Read()
        {
            if (_read != null)
            {
                _value = Mathf.Max(_value, Mathf.Clamp(_read(), 0f, 0.99f));
            }
            return _value;
        }

        public void Complete(AssetHandleState state)
        {
            if (state == AssetHandleState.Succeeded)
            {
                _value = 1f;
            }
            _read = null;
        }
    }
}
