using LiteQuark.Runtime;
using UnityEngine;

namespace LiteGamePlay
{
    public sealed class AudioManager : Singleton<AudioManager>
    {
        private AudioSource Source_;

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
            LiteRuntime.Get<AssetSystem>().LoadAsset<AudioClip>(resName, (clip) =>
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