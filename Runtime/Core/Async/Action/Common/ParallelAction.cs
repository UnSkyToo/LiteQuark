namespace LiteQuark.Runtime
{
    public class ParallelAction : CompositeAction
    {
        public override string DebugName => $"<Parallel - {Tag}>({Count})";
        
        private int _currentCount;
        
        public ParallelAction(string tag, int repeatCount, IAction[] args)
            : base(tag, repeatCount, args)
        {
            _currentCount = 0;
        }

        public override void Execute()
        {
            IsEnd = Count == 0;
            _currentCount = 0;
            
            ExecuteSubActions();
        }

        public override void Tick(float deltaTime)
        {
            var endCount = 0;

            for (var index = 0; index < Count; ++index)
            {
                if (SubActions[index].IsEnd)
                {
                    endCount++;
                    continue;
                }
                
                SubActions[index].Tick(deltaTime);

                if (SubActions[index].IsEnd)
                {
                    // SubActions[index].Dispose();
                    endCount++;
                }
            }
            
            if (endCount == Count)
            {
                _currentCount++;
                if (RepeatCount < 0 || _currentCount < RepeatCount)
                {
                    ExecuteSubActions();
                }
                else
                {
                    IsEnd = true;
                }
            }
        }

        private void ExecuteSubActions()
        {
            foreach (var action in SubActions)
            {
                action.Execute();
            }
        }
    }
}