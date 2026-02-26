namespace LiteQuark.Runtime
{
    public class ParallelAction : CompositeAction
    {
        public override string DebugName => $"<Parallel - {Tag}>({SubActionCount})";
        
        private int _currentCount;
        
        public ParallelAction(string tag, int repeatCount, IAction[] args)
            : base(tag, repeatCount, args)
        {
            _currentCount = 0;
        }

        public override void Execute()
        {
            IsDone = SubActionCount == 0;
            _currentCount = 0;
            
            ExecuteSubActions();
        }

        public override void Tick(float deltaTime)
        {
            var endCount = 0;

            for (var index = 0; index < SubActionCount; ++index)
            {
                if (SubActions[index].IsDone)
                {
                    endCount++;
                    continue;
                }
                
                SubActions[index].Tick(deltaTime);

                if (SubActions[index].IsDone)
                {
                    endCount++;
                }
            }
            
            if (endCount == SubActionCount)
            {
                _currentCount++;
                if (RepeatCount < 0 || _currentCount < RepeatCount)
                {
                    ExecuteSubActions();
                }
                else
                {
                    IsDone = true;
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