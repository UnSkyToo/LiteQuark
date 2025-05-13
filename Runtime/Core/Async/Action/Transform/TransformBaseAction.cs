using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class TransformBaseAction : BaseAction
    {
        protected readonly Transform TS;
        
        protected TransformBaseAction(Transform transform)
        {
            TS = transform;
        }

        public bool CheckSafety()
        {
            if (!IsSafety || TS != null)
            {
                return true;
            }

            IsEnd = true;
            return false;
        }
    }
}