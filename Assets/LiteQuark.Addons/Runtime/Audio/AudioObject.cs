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

        public override string DebugName => $"AudioClip_{Source?.clip?.name}";

        private AssetHandle<AudioClip> _clipHandle;

        public AudioObject(AudioType type, string path)
            : base()
        {
            Type = type;
            Path = path;
            IsLoaded = false;
        }

        public void Load(EmptyGameObjectPool pool, Transform parent, string clipPath, bool isLoop, float volume, float delay, System.Action<bool> callback)
        {
            if (IsLoaded)
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
                LoadClipAsync(pool, clipPath, isLoop, volume, delay, _clipHandle, callback).Forget();
            });
        }

        private async UniTaskVoid LoadClipAsync(EmptyGameObjectPool pool, string clipPath, bool isLoop, float volume, float delay,
            AssetHandle<AudioClip> handle, System.Action<bool> callback)
        {
            try
            {
                await handle;
                if (_clipHandle != handle)
                {
                    return;
                }

                var clip = handle.Result;
                if (clip == null)
                {
                    LLog.Warning("can't play audio : {0}", clipPath);
                    handle.Dispose();
                    _clipHandle = null;
                    pool.Recycle(Carrier);
                    Carrier = null;
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
            catch (Exception ex)
            {
                if (_clipHandle == handle)
                {
                    _clipHandle = null;
                }

                handle.Dispose();
                LLog.Error("load audio error : {0}, {1}", clipPath, ex.Message);
                LiteUtils.SafeInvoke(callback, false);
            }
        }

        public void Unload(EmptyGameObjectPool pool)
        {
            if (!IsLoaded && _clipHandle == null)
            {
                return;
            }

            if (_clipHandle != null)
            {
                _clipHandle.Dispose();
                _clipHandle = null;
            }

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
