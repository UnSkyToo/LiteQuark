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
    }
}