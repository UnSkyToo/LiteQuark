namespace LiteQuark.Runtime
{
    public class SequenceAction : CompositeAction
    {
        public override string DebugName => $"<Sequence - {Tag_}>({Index_}/{Count_})";
        
        private IAction Current_;
        protected int Index_;
        
        public SequenceAction(string tag, IAction[] args)
            : base(tag, args)
        {
            Index_ = -1;
            Current_ = null;
        }

        public override void Execute()
        {
            IsEnd = Count_ == 0;
            ActiveNextAction();
        }

        public override void Tick(float deltaTime)
        {
            if (Current_ == null)
            {
                return;
            }
            
            if (Current_.IsEnd)
            {
                ActiveNextAction();
            }
            else
            {
                Current_.Tick(deltaTime);
            }
        }

        private void ActiveNextAction()
        {
            while (!IsEnd)
            {
                Current_?.Dispose();

                Index_ = GetNextIndex();
                if (Index_ == -1)
                {
                    Current_ = null;
                    IsEnd = true;
                }
                else
                {
                    Current_ = SubActions_[Index_];
                }

                if (Current_ != null)
                {
                    Current_.Execute();

                    if (Current_.IsEnd)
                    {
                        continue;
                    }
                }

                break;
            }
        }

        protected virtual int GetNextIndex()
        {
            Index_++;
            
            if (Index_ >= Count_)
            {
                return -1;
            }

            return Index_;
        }
    }

    public class RepeatSequenceAction : SequenceAction
    {
        public RepeatSequenceAction(string tag, IAction[] args)
            : base(tag, args)
        {
        }

        protected override int GetNextIndex()
        {
            Index_++;

            if (Index_ >= Count_)
            {
                return 0;
            }

            return Index_;
        }
    }
}