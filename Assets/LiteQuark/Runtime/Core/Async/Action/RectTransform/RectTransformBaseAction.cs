using UnityEngine;

namespace LiteQuark.Runtime
{
    public abstract class RectTransformBaseAction : BaseAction
    {
        protected readonly RectTransform RT;
        
        protected RectTransformBaseAction(RectTransform transform)
        {
            RT = transform;
        }

        public bool CheckSafety()
        {
            if (!IsSafety || RT != null)
            {
                return true;
            }

            IsDone = true;
            return false;
        }
    }
}