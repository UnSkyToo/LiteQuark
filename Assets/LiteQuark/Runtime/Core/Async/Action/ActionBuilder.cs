using System.Collections.Generic;

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

        private readonly BuildType _buildType;
        private readonly string _tag;
        private readonly List<IAction> _actionList;
        private ActionBuilder _parent;

        private ActionBuilder(BuildType buildType, string tag)
        {
            _buildType = buildType;
            _tag = string.IsNullOrEmpty(tag) ? "unknown" : tag;
            _actionList = new List<IAction>();
            _parent = null;
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
            builder._parent = this;
            return builder;
        }

        public ActionBuilder EndSequence()
        {
            if (_parent == null)
            {
                return null;
            }

            _parent.Action(Flush());
            return _parent;
        }

        public ActionBuilder BeginParallel(string tag = "unknown")
        {
            var builder = Parallel(tag);
            builder._parent = this;
            return builder;
        }

        public ActionBuilder EndParallel()
        {
            if (_parent == null)
            {
                return null;
            }

            _parent.Action(Flush());
            return _parent;
        }
        
        public IAction Flush()
        {
            switch (_buildType)
            {
                case BuildType.Sequence:
                    return new SequenceAction(_tag, _actionList.ToArray());
                case BuildType.RepeatSequence:
                    return new RepeatSequenceAction(_tag, _actionList.ToArray());
                case BuildType.Parallel:
                    return new ParallelAction(_tag, _actionList.ToArray());
            }

            return null;
        }
        
        public ActionBuilder Add(IAction action)
        {
            _actionList.Add(action);
            return this;
        }
    }

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder Action(this ActionBuilder builder, IAction action)
        {
            builder.Add(action);
            return builder;
        }
    }
}