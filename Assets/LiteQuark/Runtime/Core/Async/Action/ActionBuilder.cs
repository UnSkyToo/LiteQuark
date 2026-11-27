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
        private readonly List<BaseAction> _actionList;
        private ActionBuilder _parent;
        private bool _isUnscaled;

        private ActionBuilder(BuildType buildType, string tag, int repeatCount = 1)
        {
            _buildType = buildType;
            _tag = string.IsNullOrEmpty(tag) ? "unknown" : tag;
            _repeatCount = repeatCount;
            _actionList = new List<BaseAction>();
            _parent = null;
            _isUnscaled = false;
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

            _parent.Add(Flush());
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

            _parent.Add(Flush());
            return _parent;
        }

        public ActionBuilder SetUnscaled(bool isUnscaled = true)
        {
            _isUnscaled = isUnscaled;
            return this;
        }

        public BaseAction Flush()
        {
            var subActions = _actionList.ToArray();
            BaseAction action = _buildType == BuildType.Sequence
                ? new SequenceAction(_tag, _repeatCount, subActions)
                : new ParallelAction(_tag, _repeatCount, subActions);
            return action.SetUnscaled(_isUnscaled);
        }

        public ActionBuilder Add(BaseAction action)
        {
            _actionList.Add(action);
            return this;
        }
    }
}
