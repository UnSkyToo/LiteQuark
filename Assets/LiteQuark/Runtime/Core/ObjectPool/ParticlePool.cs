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

        public override async UniTask<GameObject> Alloc(Transform parent)
        {
            var go = await base.Alloc(parent);
            if (go != null)
            {
                var particles = go.GetComponentsInChildren<ParticleSystem>();
                foreach (var particle in particles)
                {
                    particle.Play();
                }
            }
            return go;
        }
    }
}
