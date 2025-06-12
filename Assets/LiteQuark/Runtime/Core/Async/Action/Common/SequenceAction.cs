namespace LiteQuark.Runtime
{
    public class SequenceAction : CompositeAction
    {
        public override string DebugName => $"<Sequence - {Tag}>({Index}/{Count})";
        
        private IAction _current;
        protected int Index;
        
        public SequenceAction(string tag, IAction[] args)
            : base(tag, args)
        {
            Index = -1;
            _current = null;
        }

        public override void Execute()
        {
            IsEnd = Count == 0;
            ActiveNextAction();
        }

        public override void Tick(float deltaTime)
        {
            if (_current == null)
            {
                return;
            }
            
            if (_current.IsEnd)
            {
                ActiveNextAction();
            }
            else
            {
                _current.Tick(deltaTime);
                if (_current.IsEnd)
                {
                    ActiveNextAction();
                }
            }
        }

        private void ActiveNextAction()
        {
            while (!IsEnd)
            {
                _current?.Dispose();

                Index = GetNextIndex();
                if (Index == -1)
                {
                    _current = null;
                    IsEnd = true;
                }
                else
                {
                    _current = SubActions[Index];
                }

                if (_current != null)
                {
                    _current.Execute();

                    if (_current.IsEnd)
                    {
                        continue;
                    }
                }

                break;
            }
        }

        protected virtual int GetNextIndex()
        {
            Index++;
            
            if (Index >= Count)
            {
                return -1;
            }

            return Index;
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
            Index++;

            if (Index >= Count)
            {
                return 0;
            }

            return Index;
        }
    }
}