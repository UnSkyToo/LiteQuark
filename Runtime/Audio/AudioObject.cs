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

                LiteRuntime.Asset.LoadAssetAsync<AudioClip>(clipPath, (clip) =>
                {
                    if (clip == null)
                    {
                        LLog.Warning("can't play audio : {0}", clipPath);
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
                });
            });
        }

        public void Unload(EmptyGameObjectPool pool)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (Source != null && Source.clip != null)
            {
                LiteRuntime.Asset.UnloadAsset(Source.clip);
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

            if (Source.clip?.loadState != AudioDataLoadState.Loaded)
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