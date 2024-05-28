using UnityEngine;

namespace LiteQuark.Runtime
{
    public class AudioObject : BaseObject
    {
        public AudioType Type { get; private set; }
        public string Path { get; private set; }
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

        public void Load(EmptyGameObjectPool pool, Transform parent, AudioClip clip, bool isLoop, float volume, bool isMute)
        {
            if (IsLoaded)
            {
                return;
            }
            
            Carrier = pool.Alloc(parent);
            if (Carrier == null)
            {
                return;
            }

            Source = Carrier.GetOrAddComponent<AudioSource>();
            Source.clip = clip;
            Source.volume = volume;
            Source.loop = isLoop;
            Source.pitch = 1.0f;
            Source.mute = isMute;
            Carrier.name = DebugName;
            IsLoaded = true;
        }

        public void Unload(EmptyGameObjectPool pool)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (Carrier != null)
            {
                Source = null;
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

            Source.Play();
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