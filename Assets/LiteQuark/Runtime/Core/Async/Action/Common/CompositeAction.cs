namespace LiteQuark.Runtime
{
    public abstract class CompositeAction : BaseAction
    {
        protected readonly string Tag;
        protected readonly int RepeatCount;
        protected readonly BaseAction[] SubActions;
        protected readonly int SubActionCount;

        protected CompositeAction(string tag, int repeatCount, BaseAction[] args)
        {
            Tag = string.IsNullOrEmpty(tag) ? "unknown" : tag;
            RepeatCount = repeatCount;
            SubActions = args ?? System.Array.Empty<BaseAction>();
            SubActionCount = args?.Length ?? 0;
            IsDone = SubActionCount == 0;
        }

        protected internal override void Dispose()
        {
            foreach (var action in SubActions)
            {
                action.Dispose();
            }

            base.Dispose();
        }

        protected internal override void MarkAsSafe()
        {
            base.MarkAsSafe();

            foreach (var action in SubActions)
            {
                action.MarkAsSafe();
            }
        }

        public override void Cancel()
        {
            if (IsDone)
            {
                return;
            }

            base.Cancel();
            foreach (var action in SubActions)
            {
                action.Cancel();
            }
        }

        internal BaseAction[] GetSubActions()
        {
            return SubActions;
        }
    }
}
