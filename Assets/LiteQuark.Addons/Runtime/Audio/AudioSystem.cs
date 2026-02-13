using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class AudioSystem : ISystem, ITick
    {
        private bool _muteSound = false;
        private bool _muteMusic = false;
        
        private readonly Dictionary<ulong, AudioObject> _audioCache = new Dictionary<ulong, AudioObject>();
        private readonly List<AudioObject> _removeList = new List<AudioObject>();
        private EmptyGameObjectPool _pool = null;
        private Transform _root = null;
        
        public AudioSystem()
        {
            _audioCache.Clear();
            _removeList.Clear();
            _muteSound = false;
            _muteMusic = false;
        }
        
        public UniTask<bool> Initialize()
        {
            _root = UnityUtils.CreateHoldGameObject("Audio").transform;
            _pool = LiteRuntime.ObjectPool.GetEmptyGameObjectPool("Audio");
            return UniTask.FromResult(true);
        }

        public void Dispose()
        {
            foreach (var audio in _removeList)
            {
                audio.Stop();
                audio.Unload(_pool);
            }
            _removeList.Clear();

            foreach (var chunk in _audioCache)
            {
                chunk.Value.Stop();
                chunk.Value.Unload(_pool);
            }
            _audioCache.Clear();
            
            LiteRuntime.ObjectPool.RemovePool(_pool);
            
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
                _root = null;
            }
        }
        
        public void Tick(float deltaTime)
        {
            foreach (var chunk in _audioCache)
            {
                if (chunk.Value.IsEnd())
                {
                    _removeList.Add(chunk.Value);
                }
            }

            if (_removeList.Count > 0)
            {
                foreach (var audio in _removeList)
                {
                    _audioCache.Remove(audio.UniqueID);
                    audio.Unload(_pool);
                }
                _removeList.Clear();
            }
        }
        
        public AudioObject GetAudio(ulong id)
        {
            return _audioCache.GetValueOrDefault(id);
        }

        private int GetAudioCountByPath(string path)
        {
            var count = 0;
            
            foreach (var chunk in _audioCache)
            {
                if (chunk.Value.Path == path)
                {
                    count++;
                }
            }

            return count;
        }
        
        private ulong PlayAudio(AudioType type, Transform parent, string path, bool isLoop = false, int limit = 0, float volume = 1.0f, float delay = 0f)
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
            _audioCache.Add(audio.UniqueID, audio);

            audio.Load(_pool, parent, path, isLoop, volume, delay, (isLoaded) =>
            {
                if (!isLoaded)
                {
                    _removeList.Add(audio);
                    return;
                }
                
                SetupAudioOnLoad(audio);
                audio.Play();
            });
            
            return audio.UniqueID;
        }

        private void SetupAudioOnLoad(AudioObject audio)
        {
            switch (audio.Type)
            {
                case AudioType.Sound:
                    if (_muteSound)
                    {
                        audio.Mute(_muteSound);
                    }
                    break;
                case AudioType.Music:
                    if (_muteMusic)
                    {
                        audio.Mute(_muteMusic);
                    }
                    break;
            }
        }

        public ulong PlaySound(string path, bool isLoop = false, int limit = 0, float volume = 1.0f, float delay = 0f)
        {
            return PlayAudio(AudioType.Sound, _root, path, isLoop, limit, volume, delay);
        }

        public ulong PlayMusic(string path, bool isLoop = true, float volume = 1.0f, bool isOnly = true)
        {
            if (isOnly)
            {
                StopAllMusic();
            }

            return PlayAudio(AudioType.Music, _root, path, isLoop, 0, volume);
        }

        public void StopAudio(ulong id)
        {
            if (_audioCache.ContainsKey(id))
            {
                _audioCache[id].Stop();
                _removeList.Add(_audioCache[id]);
            }
        }

        public void StopAllSound()
        {
            foreach (var chunk in _audioCache)
            {
                if (chunk.Value.Type == AudioType.Sound)
                {
                    chunk.Value.Stop();
                    _removeList.Add(chunk.Value);
                }
            }
        }

        public void StopAllMusic()
        {
            foreach (var chunk in _audioCache)
            {
                if (chunk.Value.Type == AudioType.Music)
                {
                    chunk.Value.Stop();
                    _removeList.Add(chunk.Value);
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
            _muteSound = isMute;

            foreach(var chunk in _audioCache)
            {
                if (chunk.Value.Type == AudioType.Sound)
                {
                    chunk.Value.Mute(isMute);
                }
            }
        }

        public void MuteAllMusic(bool isMute)
        {
            _muteMusic = isMute;

            foreach (var chunk in _audioCache)
            {
                if (chunk.Value.Type == AudioType.Music)
                {
                    chunk.Value.Mute(isMute);
                }
            }
        }
        
        public void SetAllAudioVolume(float volume)
        {
            foreach (var chunk in _audioCache)
            {
                chunk.Value.SetVolume(volume);
            }
        }

        public void PauseAllAudio()
        {
            AudioListener.pause = true;
            
            foreach (var chunk in _audioCache)
            {
                chunk.Value.Pause();
            }
        }

        public void ResumeAllAudio()
        {
            AudioListener.pause = false;
            
            foreach (var chunk in _audioCache)
            {
                chunk.Value.Resume();
            }
        }
    }
}