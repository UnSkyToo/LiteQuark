using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class TransformBaseAction : BaseAction
    {
        protected readonly Transform TS_;
        
        protected TransformBaseAction(Transform transform)
        {
            TS_ = transform;
        }

        public bool CheckSafety()
        {
            if (!IsSafety || TS_ != null)
            {
                return true;
            }

            IsEnd = true;
            return false;
        }
    }
}