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

    public static partial class ActionBuilderExtend
    {
        public static ActionBuilder Action(this ActionBuilder builder, IAction action)
        {
            builder.Add(action);
            return builder;
        }
    }
}