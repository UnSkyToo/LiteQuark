using UnityEngine;

namespace LiteQuark.Runtime
{
    public class RectTransformSetPositionAction : RectTransformBaseAction
    {
        public override string DebugName => $"<RectTransformSetPosition>({RT_.name},{Position_})";
        
        private readonly Vector2 Position_;

        public RectTransformSetPositionAction(RectTransform transform, Vector2 position)
            : base(transform)
        {
            Position_ = position;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }

            RT_.anchoredPosition = Position_;
            IsEnd = true;
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder RectTransformSetPosition(this ActionBuilder builder, RectTransform transform, Vector2 position)
        {
            builder.Add(new RectTransformSetPositionAction(transform, position));
            return builder;
        }
    }
}