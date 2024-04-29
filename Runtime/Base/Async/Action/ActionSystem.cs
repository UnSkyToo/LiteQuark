namespace LiteQuark.Runtime
{
    public sealed class ActionSystem : ISystem, ITick
    {
        private readonly ListEx<IAction> ActionList_ = new ListEx<IAction>();
        
        public ActionSystem()
        {
            ActionList_.Clear();
        }

        public void Dispose()
        {
            ActionList_.Foreach((action) => action.Dispose());
            ActionList_.Clear();
        }

        public void Tick(float deltaTime)
        {
            ActionList_.Foreach((action, list, dt) =>
            {
                if (action.IsEnd)
                {
                    action.Dispose();
                    list.Remove(action);
                }
                else
                {
                    action.Tick(dt);
                }
            }, ActionList_, deltaTime);
        }

        public bool IsIdle()
        {
            return ActionList_.Count == 0;
        }

        public IAction FindAction(ulong id)
        {
            return ActionList_.Where(action => action.ID == id);
        }

        public void StopAction(ulong id)
        {
            var action = FindAction(id);
            action?.Stop();
        }

        public ulong AddAction(IAction action)
        {
            ActionList_.Add(action);
            action.Execute();
            return action.ID;
        }

        public ulong AddBuilder(ActionBuilder builder)
        {
            return AddAction(builder.Flush());
        }

        public ActionBuilder Sequence(string tag, bool isRepeat = false)
        {
            return ActionBuilder.Sequence(tag, isRepeat);
        }

        public ActionBuilder Parallel(string tag)
        {
            return ActionBuilder.Parallel(tag);
        }
    }
}