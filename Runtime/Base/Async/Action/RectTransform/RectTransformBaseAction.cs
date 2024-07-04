using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class RectTransformBaseAction : BaseAction
    {
        protected readonly RectTransform RT_;
        
        protected RectTransformBaseAction(RectTransform transform)
        {
            RT_ = transform;
        }

        public bool CheckSafety()
        {
            if (!IsSafety || RT_ != null)
            {
                return true;
            }

            IsEnd = true;
            return false;
        }
    }
}