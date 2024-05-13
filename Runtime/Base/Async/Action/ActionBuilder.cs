using System.Collections.Generic;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public sealed class ActionBuilder
    {
        private enum BuildType
        {
            Sequence,
            RepeatSequence,
            Parallel,
        }

        private readonly BuildType BuildType_;
        private readonly string Tag_;
        private readonly List<IAction> ActionList_;
        private ActionBuilder Parent_;

        private ActionBuilder(BuildType buildType, string tag)
        {
            BuildType_ = buildType;
            Tag_ = string.IsNullOrEmpty(tag) ? "unknown" : tag;
            ActionList_ = new List<IAction>();
            Parent_ = null;
        }
        
        public static ActionBuilder Sequence(string tag = "unknown", bool isRepeat = false)
        {
            return new ActionBuilder(isRepeat ? BuildType.RepeatSequence : BuildType.Sequence, tag);
        }

        public static ActionBuilder Parallel(string tag = "unknown")
        {
            return new ActionBuilder(BuildType.Parallel, tag);
        }

        public ActionBuilder BeginSequence(string tag = "unknown", bool isRepeat = false)
        {
            var builder = Sequence(tag, isRepeat);
            builder.Parent_ = this;
            return builder;
        }

        public ActionBuilder EndSequence()
        {
            if (Parent_ == null)
            {
                return null;
            }

            Parent_.Action(Flush());
            return Parent_;
        }

        public ActionBuilder BeginParallel(string tag = "unknown")
        {
            var builder = Parallel(tag);
            builder.Parent_ = this;
            return builder;
        }

        public ActionBuilder EndParallel()
        {
            if (Parent_ == null)
            {
                return null;
            }

            Parent_.Action(Flush());
            return Parent_;
        }
        
        public IAction Flush()
        {
            switch (BuildType_)
            {
                case BuildType.Sequence:
                    return new SequenceAction(Tag_, ActionList_.ToArray());
                case BuildType.RepeatSequence:
                    return new RepeatSequenceAction(Tag_, ActionList_.ToArray());
                case BuildType.Parallel:
                    return new ParallelAction(Tag_, ActionList_.ToArray());
            }

            return null;
        }
        
        public ActionBuilder Add(IAction action)
        {
            ActionList_.Add(action);
            return this;
        }
    }

    public static class ActionBuilderExtend
    {
        public static ActionBuilder Action(this ActionBuilder builder, IAction action)
        {
            builder.Add(action);
            return builder;
        }
        
        public static ActionBuilder WaitTime(this ActionBuilder builder, float time)
        {
            builder.Add(new WaitTimeAction(time));
            return builder;
        }
        
        public static ActionBuilder WaitFrame(this ActionBuilder builder, int frame)
        {
            builder.Add(new WaitFrameAction(frame));
            return builder;
        }
        
        public static ActionBuilder WaitUntil(this ActionBuilder builder, System.Func<bool> conditionFunc)
        {
            builder.Add(new WaitUntilAction(conditionFunc));
            return builder;
        }

        public static ActionBuilder Callback(this ActionBuilder builder, System.Action callback)
        {
            builder.Add(new CallbackAction(callback));
            return builder;
        }

        public static ActionBuilder TransformLocalMove(this ActionBuilder builder, Transform transform, Vector3 position, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformMoveAction(transform, position, time, true, isRelative, easeKind));
            return builder;
        }

        public static ActionBuilder TransformWorldMove(this ActionBuilder builder, Transform transform, Vector3 position, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformMoveAction(transform, position, time, false, isRelative, easeKind));
            return builder;
        }
        
        public static ActionBuilder TransformLocalMovePath(this ActionBuilder builder, Transform transform, Vector3[] path, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformMovePathAction(transform, path, time, true, isRelative, easeKind));
            return builder;
        }

        public static ActionBuilder TransformWorldMovePath(this ActionBuilder builder, Transform transform, Vector3[] path, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformMovePathAction(transform, path, time, false, isRelative, easeKind));
            return builder;
        }

        public static ActionBuilder TransformScale(this ActionBuilder builder, Transform transform, Vector3 scale, float time, bool isRelative = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformScaleAction(transform, scale, time, isRelative, easeKind));
            return builder;
        }

        public static ActionBuilder TransformRotate(this ActionBuilder builder, Transform transform, Quaternion rotation, float time, bool isLocal = false, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformRotateAction(transform, rotation, time, isLocal, easeKind));
            return builder;
        }

        public static ActionBuilder TransformLocalRotateAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, float angle, float time)
        {
            builder.Add(new TransformRotateAroundAction(transform, center, axis, angle, time, true));
            return builder;
        }
        
        public static ActionBuilder TransformWorldRotateAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, float angle, float time)
        {
            builder.Add(new TransformRotateAroundAction(transform, center, axis, angle, time, false));
            return builder;
        }
        
        public static ActionBuilder TransformLocalTargetRotateAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, Vector3 targetPosition, float angle, float time)
        {
            builder.Add(new TransformTargetRotateAroundAction(transform, center, axis, targetPosition, angle, time, true));
            return builder;
        }
        
        public static ActionBuilder TransformWorldTargetRotateAround(this ActionBuilder builder, Transform transform, Vector3 center, Vector3 axis, Vector3 targetPosition, float angle, float time)
        {
            builder.Add(new TransformTargetRotateAroundAction(transform, center, axis, targetPosition, angle, time, false));
            return builder;
        }

        public static ActionBuilder TransformFade(this ActionBuilder builder, Transform transform, float beginAlpha, float endAlpha, float time, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformFadeAction(transform, beginAlpha, endAlpha, time, easeKind));
            return builder;
        }

        public static ActionBuilder TransformFadeIn(this ActionBuilder builder, Transform transform, float time, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformFadeInAction(transform, time, easeKind));
            return builder;
        }

        public static ActionBuilder TransformFadeOut(this ActionBuilder builder, Transform transform, float time, EaseKind easeKind = EaseKind.Linear)
        {
            builder.Add(new TransformFadeOutAction(transform, time, easeKind));
            return builder;
        }

        public static ActionBuilder TransformShaking(this ActionBuilder builder, Transform transform, float time, float strength, int vibrato, float randomness = 90.0f)
        {
            builder.Add(new TransformShakingAction(transform, time, strength, vibrato, randomness));
            return builder;
        }

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
    }
}