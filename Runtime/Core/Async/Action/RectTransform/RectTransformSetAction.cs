using UnityEngine;

namespace LiteQuark.Runtime
{
    public class RectTransformSetPositionAction : RectTransformBaseAction
    {
        public override string DebugName => $"<RectTransformSetPosition>({RT.name},{_position})";
        
        private readonly Vector2 _position;

        public RectTransformSetPositionAction(RectTransform transform, Vector2 position)
            : base(transform)
        {
            _position = position;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }

            RT.anchoredPosition = _position;
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