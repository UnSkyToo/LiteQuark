using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class AudioSystem : ISystem
    {
        private AudioSource Source_;

        public AudioSystem()
        {
        }

        public void Dispose()
        {
        }

        private AudioSource GetSource()
        {
            if (Source_ == null)
            {
                Source_ = GameObject.FindObjectOfType<AudioSource>();
            }

            return Source_;
        }
        
        public void PlaySound(string resName)
        {
            LiteRuntime.Get<AssetSystem>().LoadAssetAsync<AudioClip>(resName, (clip) =>
            {
                if (clip == null)
                {
                    return;
                }
                
                GetSource()?.PlayOneShot(clip);
            });
        }
    }
}