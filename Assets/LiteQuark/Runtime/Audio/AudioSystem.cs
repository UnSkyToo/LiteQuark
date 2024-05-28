using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class AudioSystem : ISystem, ITick
    {
        private bool MuteSound_ = false;
        private bool MuteMusic_ = false;
        
        private readonly Dictionary<ulong, AudioObject> AudioCache_ = new Dictionary<ulong, AudioObject>();
        private readonly List<AudioObject> RemoveList_ = new List<AudioObject>();
        private readonly EmptyGameObjectPool Pool_ = null;
        private Transform Root_ = null;
        
        public AudioSystem()
        {
            if (Root_ == null)
            {
                Root_ = new GameObject("Audio").transform;
                Root_.hideFlags = HideFlags.NotEditable;
                Object.DontDestroyOnLoad(Root_);
            }
            
            Pool_ = LiteRuntime.ObjectPool.GetEmptyGameObjectPool("Audio");
            
            AudioCache_.Clear();
            RemoveList_.Clear();
            MuteSound_ = false;
            MuteMusic_ = false;
        }

        public void Dispose()
        {
            foreach (var audio in RemoveList_)
            {
                audio.Stop();
                audio.Unload(Pool_);
            }
            RemoveList_.Clear();

            foreach (var chunk in AudioCache_)
            {
                chunk.Value.Stop();
                chunk.Value.Unload(Pool_);
            }
            AudioCache_.Clear();
            
            if (Root_ != null)
            {
                Object.DestroyImmediate(Root_.gameObject);
                Root_ = null;
            }
        }
        
        public void Tick(float deltaTime)
        {
            foreach (var chunk in AudioCache_)
            {
                if (chunk.Value.IsEnd())
                {
                    RemoveList_.Add(chunk.Value);
                }
            }

            if (RemoveList_.Count > 0)
            {
                foreach (var audio in RemoveList_)
                {
                    AudioCache_.Remove(audio.UniqueID);
                    audio.Unload(Pool_);
                }
                RemoveList_.Clear();
            }
        }

        private int GetAudioCountByPath(string path)
        {
            var count = 0;
            
            foreach (var chunk in AudioCache_)
            {
                if (chunk.Value.Path == path)
                {
                    count++;
                }
            }

            return count;
        }
        
        private ulong PlayAudio(AudioType type, Transform parent, string path, bool isLoop = false, int limit = 0, float volume = 1.0f)
        {
            if (limit > 0)
            {
                var count = GetAudioCountByPath(path);
                if (count >= limit)
                {
                    return 0;
                }
            }
            
            var audio = new AudioObject(type, path);
            AudioCache_.Add(audio.UniqueID, audio);

            LiteRuntime.Asset.LoadAssetAsync<AudioClip>(path, (clip) =>
            {
                if (clip == null)
                {
                    LLog.Warning($"can't play audio : {path}");
                    RemoveList_.Add(audio);
                    return;
                }

                audio.Load(Pool_, parent, clip, isLoop, volume, false);

                switch (type)
                {
                    case AudioType.Sound:
                        if (MuteSound_)
                        {
                            audio.Mute(MuteSound_);
                        }
                        break;
                    case AudioType.Music:
                        if (MuteMusic_)
                        {
                            audio.Mute(MuteMusic_);
                        }
                        break;
                }

                audio.Play();
            });

            return audio.UniqueID;
        }

        public ulong PlaySound(string path, bool isLoop = false, int limit = 0, float volume = 1.0f)
        {
            return PlayAudio(AudioType.Sound, Root_, path, isLoop, limit, volume);
        }

        public ulong PlayMusic(string path, bool isLoop = true, float volume = 1.0f, bool isOnly = true)
        {
            if (isOnly)
            {
                StopAllMusic();
            }

            return PlayAudio(AudioType.Music, Root_, path, isLoop, 0, volume);
        }

        public void StopAudio(ulong id)
        {
            if (AudioCache_.ContainsKey(id))
            {
                AudioCache_[id].Stop();
                RemoveList_.Add(AudioCache_[id]);
            }
        }

        public void StopAllSound()
        {
            foreach (var chunk in AudioCache_)
            {
                if (chunk.Value.Type == AudioType.Sound)
                {
                    chunk.Value.Stop();
                    RemoveList_.Add(chunk.Value);
                }
            }
        }

        public void StopAllMusic()
        {
            foreach (var chunk in AudioCache_)
            {
                if (chunk.Value.Type == AudioType.Music)
                {
                    chunk.Value.Stop();
                    RemoveList_.Add(chunk.Value);
                }
            }
        }

        public void MuteAllAudio(bool isMute)
        {
            MuteAllSound(isMute);
            MuteAllMusic(isMute);
        }

        public void MuteAllSound(bool isMute)
        {
            MuteSound_ = isMute;

            foreach(var chunk in AudioCache_)
            {
                if (chunk.Value.Type == AudioType.Sound)
                {
                    chunk.Value.Mute(isMute);
                }
            }
        }

        public void MuteAllMusic(bool isMute)
        {
            MuteMusic_ = isMute;

            foreach (var chunk in AudioCache_)
            {
                if (chunk.Value.Type == AudioType.Music)
                {
                    chunk.Value.Mute(isMute);
                }
            }
        }
    }
}