using System;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [DisallowMultipleComponent]
    [Serializable]
    public class EffectBinder : MonoBehaviour
    {
        public bool IsLoop;
        public float LifeTime;
        public float RetainTime;

        public ParticleSystem[] Particles;
        public Animator[] Animators;
        public TrailRenderer[] Trails;
        
        private float[] TrailsTime_;

        public bool IsEmpty()
        {
            return Particles.Length == 0 && Animators.Length == 0 && Trails.Length == 0;
        }
        
        public void Play(float speed)
        {
            if (Animators != null)
            {
                foreach(var animator in Animators)
                {
                    if (animator != null && animator.gameObject.activeSelf)
                    {
                        animator.speed = speed;
                        for (var layer = 0; layer < animator.layerCount; ++layer)
                        {
                            var info = animator.GetCurrentAnimatorStateInfo(layer);
                            animator.playbackTime = 0;
                            animator.Play(info.fullPathHash, layer, 0);
                        }
                    }
                }
            }

            if (Particles != null)
            {
                foreach (var particle in Particles)
                {
                    if (particle != null && particle.gameObject.activeSelf)
                    {
                        var main = particle.main;
                        main.simulationSpeed = speed;
                        particle.time = 0;
                        particle.Stop(true);
                        particle.Clear(true);
                        particle.Play(true);
                    }
                }
            }
        }

        public void Stop()
        {
            if (Animators != null)
            {
                foreach (var animator in Animators)
                {
                    if (animator != null)
                    {
                        animator.playbackTime = 0;
                        animator.StopPlayback();
                    }
                }
            }
            
            if (Particles != null)
            {
                foreach (var particle in Particles)
                {
                    if (particle != null)
                    {
                        particle.time = 0;
                        particle.Stop(true);
                        particle.Clear(true);
                    }
                }
            }
            
            if (Trails != null)
            {
                foreach (var trail in Trails)
                {
                    if (trail != null)
                    {
                        trail.time = 0;
                    }
                }
            }
        }
        
        public void PlayAnimation(string stateName, int layer)
        {
            if (Animators != null)
            {
                foreach (var animator in Animators)
                {
                    if (animator != null)
                    {
                        animator.Play(stateName, layer);
                    }
                }
            }
        }
        
        public void SetSpeed(float speed)
        {
            if (Animators != null)
            {
                foreach(var animator in Animators)
                {
                    if (animator != null)
                    {
                        animator.speed = speed;
                    }
                }
            }

            if (Particles != null)
            {
                foreach (var particle in Particles)
                {
                    if (particle != null && particle.gameObject.activeSelf)
                    {
                        var main = particle.main;
                        main.simulationSpeed = speed;
                    }
                }
            }

            if (Trails != null)
            {
                if (TrailsTime_ == null)
                {
                    TrailsTime_ = new float[Trails.Length];
                    for (var i = 0; i < Trails.Length; ++i)
                    {
                        var trail = Trails[i];
                        if (trail != null)
                        {
                            TrailsTime_[i] = trail.time;
                        }
                    }
                }

                for (var i = 0; i < Trails.Length; ++i)
                {
                    var trail = Trails[i];
                    if (trail != null)
                    {
                        trail.time = speed < 0.0001f ? int.MaxValue : TrailsTime_[i] / speed;
                    }
                }
            }
        }

        public void UpdateInfo()
        {
            Particles = GetComponentsInChildren<ParticleSystem>();
            Animators = GetComponentsInChildren<Animator>();

            IsLoop = false;
            var time = 0f;
            
            foreach (var particle in Particles)
            {
                time = Mathf.Max(particle.main.duration, time);
                if (particle.main.loop)
                {
                    IsLoop = true;
                    break;
                }
            }
            
            foreach (var animator in Animators)
            {
                if (animator.runtimeAnimatorController == null)
                {
                    continue;
                }
                
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    time = Mathf.Max(clip.length, time);
                    if (clip.isLooping)
                    {
                        IsLoop = true;
                        break;
                    }
                }
            }

            if (Mathf.Approximately(time, 0f))
            {
                IsLoop = true;
            }
            
            LifeTime = IsLoop ? 0f : time;
            RetainTime = Mathf.Max(0.5f, LifeTime);
        }
    }
}