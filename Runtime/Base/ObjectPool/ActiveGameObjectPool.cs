using UnityEngine;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// Use SetActive replace change position for recycle game object
    /// </summary>
    public class ActiveGameObjectPool : BaseGameObjectPool
    {
        public ActiveGameObjectPool()
            : base()
        {
        }

        protected override void OnRelease(GameObject go)
        {
            go.SetActive(false);
            go.transform.SetParent(Parent_, false);
        }

        public override GameObject Alloc(Transform parent)
        {
            var go = base.Alloc(parent);
            go.SetActive(true);
            return go;
        }
    }
}