using UnityEngine;

namespace LiteQuark.Runtime
{
    public class EffectObject : BaseObject, ITick, IDispose
    {
        public override string DebugName => $"Effect<{UniqueID}>";

        private readonly EffectCreateInfo Info_;
        private EffectState State_;
        private GameObject Go_;
        private EffectBinder Binder_;
        
        private float CurSpeed_ = 1f;
        private float LastSpeed_ = 1f;
        private float LastTimeScale_ = 1f;
        private float Time_ = 0f;

        public bool IsLoop => Info_.IsLoop || Binder_.IsLoop;
        public float LifeTime => Info_.LifeTime > 0f ? Info_.LifeTime : Binder_.LifeTime;
        public bool IsValid => State_ is > EffectState.Created and < EffectState.Finished;
        public bool IsEnd => State_ == EffectState.Finished;

        public EffectObject(EffectCreateInfo info)
        {
            Info_ = info;
            State_ = EffectState.Created;
            
            LiteRuntime.ObjectPool.GetActiveGameObjectPool(Info_.Path).Alloc(Info_.Parent, OnLoad);
        }

        public void Dispose()
        {
            if (Go_ != null)
            {
                LiteRuntime.ObjectPool.GetActiveGameObjectPool(Info_.Path).Recycle(Go_);
                Go_ = null;
            }

            State_ = EffectState.Destroyed;
        }

        public void Tick(float deltaTime)
        {
            if (!IsValid)
            {
                return;
            }

            switch (State_)
            {
                case EffectState.Created:
                    break;
                case EffectState.Pause:
                    break;
                case EffectState.Playing:
                    Time_ -= deltaTime * CurSpeed_;
                    if (Time_ <= 0f)
                    {
                        if (!IsLoop)
                        {
                            Stop();
                        }
                        else
                        {
                            Play(CurSpeed_);
                        }
                    }
                    break;
                case EffectState.Finishing:
                    Time_ -= deltaTime * CurSpeed_;
                    if (Time_ <= 0f)
                    {
                        State_ = EffectState.Finished;
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
            if (State_ == EffectState.Created)
            {
                State_ = EffectState.Pause;
                Go_ = go;

                Binder_ = Go_.GetComponent<EffectBinder>();
                if (Binder_ == null)
                {
#if UNITY_EDITOR
                    LLog.Warning($"Effect {Info_.Path} is not cached!");
#endif
                    Binder_ = Go_.AddComponent<EffectBinder>();
                    Binder_.UpdateInfo();
                }
                else if (Binder_.IsEmpty())
                {
#if UNITY_EDITOR
                    LLog.Warning($"Effect {Info_.Path} is not cached!");
#endif
                    Binder_.UpdateInfo();
                }
                
                if (Info_.Parent != null)
                {
                    if ((Info_.Space & EffectSpace.LocalPosition) != 0)
                    {
                        go.transform.localPosition = Info_.Position;
                    }
                    else
                    {
                        go.transform.position = Info_.Position;
                    }
                
                    if ((Info_.Space & EffectSpace.LocalRotation) != 0)
                    {
                        go.transform.localRotation = Info_.Rotation;
                    }
                    else
                    {
                        go.transform.rotation = Info_.Rotation;
                    }
                }
                else
                {
                    go.transform.position = Info_.Position;
                    go.transform.rotation = Info_.Rotation;
                }
                    
                go.transform.localScale = Vector3.one * Info_.Scale;

                if (Info_.Order > 0)
                {
                    UnityUtils.AddSortingOrder(Go_, Info_.Order);
                }
                
                Play(Info_.Speed);
            }
        }
        
        public void SetSpeed(float speed)
        {
            if (Mathf.Abs(CurSpeed_ - speed) > 0.0001f && IsValid)
            {
                LastSpeed_ = CurSpeed_;
                CurSpeed_ = speed;
                Binder_.SetSpeed(CurSpeed_);
            }
        }

        public void ResetSpeed()
        {
            if (Mathf.Abs(CurSpeed_ - LastSpeed_) > 0.0001f && IsValid)
            {
                CurSpeed_ = LastSpeed_;
                Binder_.SetSpeed(CurSpeed_);
            }
        }

        public void SetTime(float time)
        {
            if (IsValid)
            {
                Binder_.SetTime(time);
            }
        }
        
        public void Play(float speed)
        {
            if (IsValid)
            {
                State_ = EffectState.Playing;
                LastSpeed_ = CurSpeed_;
                CurSpeed_ = speed;
                Time_ = LifeTime;
                Binder_.Play(speed);
            }
        }

        public void PlayAnimation(string stateName, int layer, float speed)
        {
            if (IsValid)
            {
                State_ = EffectState.Playing;
                LastSpeed_ = CurSpeed_;
                CurSpeed_ = speed;
                Time_ = LifeTime;
                Binder_.PlayAnimation(stateName, layer);
            }
        }

        public void Stop()
        {
            State_ = EffectState.Finishing;
            Time_ = Binder_.RetainTime;
            Binder_.Stop();
        }
    }
}