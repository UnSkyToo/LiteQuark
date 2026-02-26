using UnityEngine;

namespace LiteQuark.Runtime
{
    public class PositionGameObjectPool : ActiveGameObjectPool
    {
        private static readonly Vector3 InvalidPosition = new Vector3(-9999, -9999, -9999);
        
        public PositionGameObjectPool()
            : base()
        {
        }
        
        protected override void OnRelease(GameObject go)
        {
            go.transform.SetParent(Parent, false);
            go.transform.localPosition = InvalidPosition;
        }
    }
}