namespace LiteQuark.Runtime
{
    public class SequenceAction : CompositeAction
    {
        public override string DebugName => $"<Sequence - {Tag}>({_index}/{SubActionCount})";

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
            IsDone = SubActionCount == 0;
            _currentCount = 0;
            
            ActivateNextAction();
        }

        public override void Tick(float deltaTime)
        {
            if (_current == null)
            {
                return;
            }
            
            if (_current.IsDone)
            {
                ActivateNextAction();
            }
            else
            {
                _current.Tick(deltaTime);
                if (_current.IsDone)
                {
                    ActivateNextAction();
                }
            }
        }

        private void ActivateNextAction()
        {
            while (!IsDone)
            {
                _index = GetNextIndex();
                if (_index == -1)
                {
                    _current = null;
                    IsDone = true;
                }
                else
                {
                    _current = SubActions[_index];
                }

                if (_current != null)
                {
                    _current.Execute();

                    if (_current.IsDone)
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
            
            if (_index >= SubActionCount)
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