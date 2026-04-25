using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public class AudioObject : BaseObject
    {
        public AudioType Type { get; private set; }
        public string Path { get; private set; }
        public float Delay { get; private set; }
        public bool IsLoaded { get; private set; }
        public AudioSource Source { get; private set; }
        public GameObject Carrier { get; private set; }
        private AssetHandle<AudioClip> _clipHandle;

        public override string DebugName => $"AudioClip_{Source?.clip?.name}";

        public AudioObject(AudioType type, string path)
            : base()
        {
            Type = type;
            Path = path;
            IsLoaded = false;
        }

        public void Load(EmptyGameObjectPool pool, Transform parent, string clipPath, bool isLoop, float volume, float delay, System.Action<bool> callback)
        {
            if (IsLoaded || _clipHandle != null)
            {
                return;
            }

            pool.Alloc(parent, (go) =>
            {
                Carrier = go;
                if (Carrier == null)
                {
                    LiteUtils.SafeInvoke(callback, false);
                    return;
                }

                _clipHandle = LiteRuntime.Asset.LoadAssetHandle<AudioClip>(clipPath);
                LoadClipAsync(pool, _clipHandle, clipPath, isLoop, volume, delay, callback).Forget();
            });
        }

        private async UniTaskVoid LoadClipAsync(EmptyGameObjectPool pool, AssetHandle<AudioClip> handle,
            string clipPath, bool isLoop, float volume, float delay, System.Action<bool> callback)
        {
            try
            {
                var clip = await handle.Task;
                if (clip == null)
                {
                    LLog.Warning("can't play audio : {0}", clipPath);
                    handle.Dispose();
                    if (_clipHandle == handle)
                    {
                        _clipHandle = null;
                    }
                    if (Carrier != null)
                    {
                        pool.Recycle(Carrier);
                        Carrier = null;
                    }
                    LiteUtils.SafeInvoke(callback, false);
                    return;
                }
                    
                Source = Carrier.GetOrAddComponent<AudioSource>();
                Source.clip = clip;
                Source.volume = Mathf.Clamp01(volume);
                Source.loop = isLoop;
                Source.pitch = 1.0f;
                Carrier.name = DebugName;
                Delay = delay;
                IsLoaded = true;
                LiteUtils.SafeInvoke(callback, true);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Unload(EmptyGameObjectPool pool)
        {
            if (!IsLoaded && _clipHandle == null && Carrier == null)
            {
                return;
            }

            _clipHandle?.Dispose();
            _clipHandle = null;

            if (Source != null)
            {
                Source.clip = null;
                Source = null;
            }

            if (Carrier != null)
            {
                pool.Recycle(Carrier);
                Carrier = null;
            }

            IsLoaded = false;
        }

        public bool IsValid()
        {
            return IsLoaded && Source != null;
        }

        public bool IsEnd()
        {
            if (!IsValid())
            {
                return false;
            }

            return !Source.isPlaying;
        }

        public bool Play()
        {
            if (!IsValid())
            {
                return false;
            }

            if (Delay > 0)
            {
                Source.PlayDelayed(Delay);
            }
            else
            {
                Source.Play();
            }
            
            return true;
        }

        public bool Mute(bool isMute)
        {
            if (!IsValid())
            {
                return false;
            }

            Source.mute = isMute;
            return true;
        }

        public bool Stop()
        {
            if (!IsValid())
            {
                return false;
            }

            Source.Stop();
            return true;
        }

        public void Pause()
        {
            if (!IsValid())
            {
                return;
            }
            
            Source.Pause();
        }

        public void Resume()
        {
            if (!IsValid())
            {
                return;
            }
            
            Source.UnPause();
        }

        public bool SetVolume(float volume)
        {
            if (!IsValid())
            {
                return false;
            }

            Source.volume = volume;
            return true;
        }
    }
}
