using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public sealed class ActionBuilder
    {
        private enum BuildType
        {
            Sequence,
            Parallel,
        }

        private readonly BuildType _buildType;
        private readonly string _tag;
        private readonly int _repeatCount;
        private readonly List<IAction> _actionList;
        private ActionBuilder _parent;

        private ActionBuilder(BuildType buildType, string tag, int repeatCount = 1)
        {
            _buildType = buildType;
            _tag = string.IsNullOrEmpty(tag) ? "unknown" : tag;
            _repeatCount = repeatCount;
            _actionList = new List<IAction>();
            _parent = null;
        }
        
        public static ActionBuilder Sequence(string tag = "unknown", int repeatCount = 1)
        {
            return new ActionBuilder(BuildType.Sequence, tag, repeatCount);
        }

        public static ActionBuilder Parallel(string tag = "unknown", int repeatCount = 1)
        {
            return new ActionBuilder(BuildType.Parallel, tag, repeatCount);
        }

        public ActionBuilder BeginSequence(string tag = "unknown", int repeatCount = 1)
        {
            var builder = Sequence(tag, repeatCount);
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

        public ActionBuilder BeginParallel(string tag = "unknown", int repeatCount = 1)
        {
            var builder = Parallel(tag, repeatCount);
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
                    return new SequenceAction(_tag, _repeatCount, _actionList.ToArray());
                case BuildType.Parallel:
                    return new ParallelAction(_tag, _repeatCount, _actionList.ToArray());
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