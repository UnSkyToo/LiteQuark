namespace LiteQuark.Runtime
{
    public class ParallelAction : CompositeAction
    {
        public override string DebugName => $"<Parallel - {Tag}>({SubActionCount})";

        private int _currentCount;

        public ParallelAction(string tag, int repeatCount, BaseAction[] args)
            : base(tag, repeatCount, args)
        {
            _currentCount = 0;
        }

        protected internal override void Execute()
        {
            IsDone = SubActionCount == 0;
            _currentCount = 0;

            ExecuteSubActions();
        }

        protected internal override void Tick(float deltaTime)
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
                if (IsDone)
                {
                    return;
                }

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
            for (var index = 0; index < SubActionCount && !IsDone; ++index)
            {
                SubActions[index].Execute();
            }
        }
    }
}
