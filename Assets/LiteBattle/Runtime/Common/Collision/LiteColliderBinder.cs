using UnityEngine;

namespace LiteBattle.Runtime
{
    public enum LiteColliderType
    {
        None,
        Box,
        Sphere,
    }
    
    public class LiteColliderBinder : MonoBehaviour
    {   
        public ulong UniqueID { get; set; }
        public LiteColliderType ColliderType { get; set; }

        private void Awake()
        {
            var goCollider = GetComponent<Collider>();
            if (goCollider is BoxCollider)
            {
                ColliderType = LiteColliderType.Box;
            }
            else if (goCollider is SphereCollider)
            {
                ColliderType = LiteColliderType.Sphere;
            }
            else
            {
                ColliderType = LiteColliderType.None;
            }
        }
    }
}