using UnityEngine;

namespace LiteQuark.Runtime
{
    public struct EffectCreateInfo
    {
        public Transform Parent { get; }
        public string Path { get; }
        public EffectSpace Space { get; }
        public Vector3 Position { get; }
        public float Scale { get; }
        public Quaternion Rotation { get; }
        public float Speed { get; }
        public bool IsLoop { get; }
        public float LifeTime { get; }
        public int Order { get; }

        public EffectCreateInfo(Transform parent, string path, EffectSpace space, Vector3 position, float scale, Quaternion rotation, float speed = 1f, bool isLoop = false, float lifeTime = 0f, int order = -1)
        {
            Parent = parent;
            Path = path;
            Space = space;
            Position = position;
            Scale = scale;
            Rotation = rotation;
            Speed = speed;
            IsLoop = isLoop;
            LifeTime = lifeTime;
            Order = order;
        }
    }
}