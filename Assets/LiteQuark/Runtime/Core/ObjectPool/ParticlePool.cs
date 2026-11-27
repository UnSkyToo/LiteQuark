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

        protected override void OnAlloc(GameObject go)
        {
            base.OnAlloc(go);
            if (go == null)
            {
                return;
            }

            var particles = go.GetComponentsInChildren<ParticleSystem>();
            foreach (var particle in particles)
            {
                particle.Play();
            }
        }
    }
}
