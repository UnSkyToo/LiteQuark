using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public class ParticlePool : ActiveGameObjectPool
    {
        public ParticlePool()
            : base()
        {
        }
        
        protected override void OnRelease(GameObject go)
        {
            var particles = go.GetComponentsInChildren<ParticleSystem>();
            foreach (var particle in particles)
            {
                particle.Stop();
            }
            
            base.OnRelease(go);
        }

        public override void Alloc(Transform parent, System.Action<GameObject> callback)
        {
            base.Alloc(parent, (go) =>
            {
                var particles = go.GetComponentsInChildren<ParticleSystem>();
                foreach (var particle in particles)
                {
                    particle.Play();
                }

                callback?.Invoke(go);
            });
        }

        public override UniTask<GameObject> Alloc(Transform parent)
        {
            var tcs = new UniTaskCompletionSource<GameObject>();
            Alloc(parent, (go) =>
            {
                tcs.TrySetResult(go);
            });
            return tcs.Task;
        }
    }
}