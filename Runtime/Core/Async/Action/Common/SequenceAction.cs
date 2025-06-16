namespace LiteQuark.Runtime
{
    public class SequenceAction : CompositeAction
    {
        public override string DebugName => $"<Sequence - {Tag}>({_index}/{Count})";

        private int _currentCount;
        private IAction _current;
        private int _index;
        
        public SequenceAction(string tag, int repeatCount, IAction[] args)
            : base(tag, repeatCount, args)
        {
            _index = -1;
            _currentCount = 0;
            _current = null;
        }

        public override void Execute()
        {
            IsEnd = Count == 0;
            _currentCount = 0;
            
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

                _index = GetNextIndex();
                if (_index == -1)
                {
                    _current = null;
                    IsEnd = true;
                }
                else
                {
                    _current = SubActions[_index];
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
            _index++;
            
            if (_index >= Count)
            {
                _currentCount++;
                if (RepeatCount < 0 || _currentCount < RepeatCount)
                {
                    return 0;
                }
                
                return -1;
            }

            return _index;
        }
    }
}