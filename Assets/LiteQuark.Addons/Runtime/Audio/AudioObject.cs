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
        public GameObject Carrier => _carrierLease?.Value;

        public override string DebugName => $"AudioClip_{Source?.clip?.name}";

        private AssetHandle<AudioClip> _clipHandle;
        private GameObjectLease _carrierLease;

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

            pool.Alloc(parent, lease =>
            {
                _carrierLease = lease;
                if (lease == null)
                {
                    LiteUtils.SafeInvoke(callback, false);
                    return;
                }

                try
                {
                    _clipHandle = LiteRuntime.Asset.LoadAssetHandle<AudioClip>(clipPath);
                }
                catch (Exception ex)
                {
                    Unload();
                    LLog.Error("load audio error : {0}, {1}", clipPath, ex.Message);
                    LiteUtils.SafeInvoke(callback, false);
                    return;
                }
                LoadClipAsync(clipPath, isLoop, volume, delay, _clipHandle, callback).Forget();
            });
        }

        private async UniTaskVoid LoadClipAsync(string clipPath, bool isLoop, float volume, float delay,
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
                    Unload();
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
                Unload();
                LLog.Error("load audio error : {0}, {1}", clipPath, ex.Message);
                LiteUtils.SafeInvoke(callback, false);
            }
        }

        public void Unload()
        {
            var handle = _clipHandle;
            var source = Source;
            var carrierLease = _carrierLease;
            _clipHandle = null;
            Source = null;
            _carrierLease = null;
            IsLoaded = false;

            if (source != null)
            {
                source.Stop();
                source.clip = null;
            }

            handle?.Dispose();

            carrierLease?.Dispose();
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
