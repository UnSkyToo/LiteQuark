using UnityEngine;

namespace LiteQuark.Runtime
{
    public class EffectObject : BaseObject, ITick, IDispose
    {
        public override string DebugName => $"Effect<{UniqueID}>";

        private readonly EffectCreateInfo _info;
        private EffectState _state;
        private GameObject _go;
        private EffectBinder _binder;
        
        private float _curSpeed = 1f;
        private float _lastSpeed = 1f;
        private float _lastTimeScale = 1f;
        private float _time = 0f;

        public bool IsLoop => _info.IsLoop || (_binder?.IsLoop ?? false);
        public float LifeTime => _info.LifeTime > 0f ? _info.LifeTime : _binder?.LifeTime ?? 0f;
        public bool IsValid => _state is > EffectState.Created and < EffectState.Finished;
        public bool IsDone => _state == EffectState.Finished;

        public EffectObject(EffectCreateInfo info)
        {
            _info = info;
            _state = EffectState.Created;
            
            LiteRuntime.ObjectPool.GetActiveGameObjectPool(_info.Path).Alloc(_info.Parent, OnLoad);
        }

        public void Dispose()
        {
            if (_go != null)
            {
                if (_info.Order > 0)
                {
                    UnityUtils.AddSortingOrder(_go, -_info.Order);
                }
                
                LiteRuntime.ObjectPool.GetActiveGameObjectPool(_info.Path).Recycle(_go);
                _go = null;
            }
            else
            {
                LLog.Warning("Effect {0} parent is destroyed, not recycle!", _info.Path);
            }

            _state = EffectState.Destroyed;
        }

        public void Tick(float deltaTime)
        {
            if (!IsValid)
            {
                return;
            }

            switch (_state)
            {
                case EffectState.Created:
                    break;
                case EffectState.Pause:
                    break;
                case EffectState.Playing:
                    _time -= deltaTime * _curSpeed;
                    if (_time <= 0f)
                    {
                        if (!IsLoop)
                        {
                            LiteUtils.SafeInvoke(_info.CompleteCallback);
                            Stop();
                        }
                    }
                    break;
                case EffectState.Finishing:
                    _time -= deltaTime * _curSpeed;
                    if (_time <= 0f)
                    {
                        _state = EffectState.Finished;
                    }
                    break;
                case EffectState.Finished:
                    break;
                case EffectState.Destroyed:
                    break;
            }
        }

        private void OnLoad(GameObject go)
        {
            if (_state == EffectState.Created)
            {
                _state = EffectState.Pause;
                Setup(go);
                Play(_info.Speed);
            }
            else
            {
                if (go != null)
                {
                    LiteRuntime.ObjectPool.GetActiveGameObjectPool(_info.Path).Recycle(go);
                }
            }
        }

        private void Setup(GameObject go)
        {
            _go = go;
            
            SetupBinder(go);
            SetupTransform(go);

            if (_info.Order > 0)
            {
                UnityUtils.AddSortingOrder(go, _info.Order);
            }

            if (!string.IsNullOrWhiteSpace(_info.LayerName))
            {
                UnityUtils.ChangeSortingLayerName(go, _info.LayerName);
            }
        }

        private void SetupBinder(GameObject go)
        {
            _binder = go.GetComponent<EffectBinder>();
            if (_binder == null)
            {
#if UNITY_EDITOR
                LLog.Warning("Effect {0} is not cached!", _info.Path);
#endif
                _binder = go.AddComponent<EffectBinder>();
                _binder.UpdateInfo();
            }
            else if (_binder.IsEmpty())
            {
#if UNITY_EDITOR
                LLog.Warning("Effect {0} is not cached!", _info.Path);
#endif
                _binder.UpdateInfo();
            }
        }

        private void SetupTransform(GameObject go)
        {
            if (_info.Parent != null)
            {
                if ((_info.Space & EffectSpace.LocalPosition) != 0)
                {
                    go.transform.localPosition = _info.Position;
                }
                else
                {
                    go.transform.position = _info.Position;
                }
                
                if ((_info.Space & EffectSpace.LocalRotation) != 0)
                {
                    go.transform.localRotation = _info.Rotation;
                }
                else
                {
                    go.transform.rotation = _info.Rotation;
                }
            }
            else
            {
                go.transform.position = _info.Position;
                go.transform.rotation = _info.Rotation;
            }
                    
            go.transform.localScale = Vector3.one * _info.Scale;
        }
        
        public void SetSpeed(float speed)
        {
            if (Mathf.Abs(_curSpeed - speed) > 0.0001f && IsValid)
            {
                _lastSpeed = _curSpeed;
                _curSpeed = speed;
                _binder?.SetSpeed(_curSpeed);
            }
        }

        public void ResetSpeed()
        {
            if (Mathf.Abs(_curSpeed - _lastSpeed) > 0.0001f && IsValid)
            {
                _curSpeed = _lastSpeed;
                _binder?.SetSpeed(_curSpeed);
            }
        }

        public void SetTime(float time)
        {
            if (IsValid)
            {
                _binder?.SetTime(time);
            }
        }
        
        public void Play(float speed)
        {
            if (IsValid)
            {
                _state = EffectState.Playing;
                _lastSpeed = _curSpeed;
                _curSpeed = speed;
                _time = LifeTime;
                _binder?.Play(speed);
            }
        }

        public void PlayAnimation(string stateName, int layer, float speed)
        {
            if (IsValid)
            {
                _state = EffectState.Playing;
                _lastSpeed = _curSpeed;
                _curSpeed = speed;
                _time = LifeTime;
                _binder?.PlayAnimation(stateName, layer);
            }
        }

        public void Stop()
        {
            _state = EffectState.Finishing;
            _time = _binder?.RetainTime ?? 0f;
            _binder?.Stop();
        }
    }
}