using UnityEngine;

namespace LiteQuark.Runtime
{
    public class TransformSetPositionAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSet{(_isLocal ? "Local" : "World")}Position>({TS.name},{_position})";
        
        private readonly Vector3 _position;
        private readonly bool _isLocal;

        public TransformSetPositionAction(Transform transform, Vector3 position, bool isLocal = true)
            : base(transform)
        {
            _position = position;
            _isLocal = isLocal;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            if (_isLocal)
            {
                TS.localPosition = _position;
            }
            else
            {
                TS.position = _position;
            }

            IsEnd = true;
        }
    }
    
    public class TransformSetScaleAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSetScale>({TS.name},{_scale})";
        
        private readonly Vector3 _scale;

        public TransformSetScaleAction(Transform transform, Vector3 scale)
            : base(transform)
        {
            _scale = scale;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            TS.localScale = _scale;
            IsEnd = true;
        }
    }
    
    public class TransformSetRotationAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSet{(_isLocal ? "Local" : "World")}Rotation>({TS.name},{_rotation})";
        
        private readonly Quaternion _rotation;
        private readonly bool _isLocal;

        public TransformSetRotationAction(Transform transform, Quaternion rotation, bool isLocal = true)
            : base(transform)
        {
            _rotation = rotation;
            _isLocal = isLocal;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            if (_isLocal)
            {
                TS.localRotation = _rotation;
            }
            else
            {
                TS.rotation = _rotation;
            }

            IsEnd = true;
        }
    }
    
    public class TransformSetRotationAroundAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSetRotationAround>({TS.name},{_center},{_axis},{_angle})";
        
        private readonly Vector3 _center;
        private readonly Vector3 _axis;
        private readonly float _angle;

        public TransformSetRotationAroundAction(Transform transform, Vector3 center, Vector3 axis, float angle)
            : base(transform)
        {
            _center = center;
            _axis = axis;
            _angle = angle;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            TS.RotateAround(_center, _axis, _angle);
            IsEnd = true;
        }
    }
    
    public class TransformSetAlphaAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSetAlpha>({TS.name},{_alpha})";
        
        private readonly IAlphaBox _alphaBox;
        private readonly float _alpha;

        public TransformSetAlphaAction(Transform transform, float alpha, IAlphaBox box = null)
            : base(transform)
        {
            _alphaBox = box ?? new AlphaBox(transform);
            _alpha = alpha;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            _alphaBox.SetAlpha(_alpha);
            IsEnd = true;
        }
    }
    
    public class TransformSetActiveAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSetActive>({TS.name},{_value})";
        
        private readonly bool _value;

        public TransformSetActiveAction(Transform transform, bool value)
            : base(transform)
        {
            _value = value;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            TS.gameObject.SetActive(_value);
            IsEnd = true;
        }
    }
    
    public class TransformSetParentAction : TransformBaseAction
    {
        public override string DebugName => $"<TransformSetParent>({TS.name},{_worldStay})";

        private readonly Transform _parent;
        private readonly bool _worldStay;

        public TransformSetParentAction(Transform transform, Transform parent, bool worldStay)
            : base(transform)
        {
            _parent = parent;
            _worldStay = worldStay;
        }

        public override void Execute()
        {
            if (!CheckSafety())
            {
                return;
            }
            
            TS.SetParent(_parent, _worldStay);
            IsEnd = true;
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder TransformSetLocalPosition(this ActionBuilder builder, Transform transform, Vector3 position)
        {
            builder.Add(new TransformSetPositionAction(transform, position, true));
            return builder;
        }

        public static ActionBuilder TransformSetWorldPosition(this ActionBuilder builder, Transform transform, Vector3 position)
        {
            builder.Add(new TransformSetPositionAction(transform, position, false));
            return builder;
        }

        public static ActionBuilder TransformSetScale(this ActionBuilder builder, Transform transform, Vector3 scale)
        {
            builder.Add(new TransformSetScaleAction(transform, scale));
            return builder;
        }

        public static ActionBuilder TransformSetLocalRotation(this ActionBuilder builder, Transform transform, Quaternion rotation)
        {
            builder.Add(new TransformSetRotationAction(transform, rotation, true));
            return builder;
        }

        public static ActionBuilder TransformSetWorldRotation(this ActionBuilder builder, Transform transform, Quaternion rotation)
        {
            builder.Add(new TransformSetRotationAction(transform, rotation, false));
            return builder;
        }

        public static ActionBuilder TransformSetRotationAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, float angle)
        {
            builder.Add(new TransformSetRotationAroundAction(transform, center, axis, angle));
            return builder;
        }

        public static ActionBuilder TransformSetAlpha(this ActionBuilder builder, Transform transform, float alpha)
        {
            builder.Add(new TransformSetAlphaAction(transform, alpha));
            return builder;
        }

        public static ActionBuilder TransformSetActive(this ActionBuilder builder, Transform transform, bool value)
        {
            builder.Add(new TransformSetActiveAction(transform, value));
            return builder;
        }

        public static ActionBuilder TransformSetParent(this ActionBuilder builder, Transform transform, Transform parent, bool worldStay = true)
        {
            builder.Add(new TransformSetParentAction(transform, parent, worldStay));
            return builder;
        }
    }
}