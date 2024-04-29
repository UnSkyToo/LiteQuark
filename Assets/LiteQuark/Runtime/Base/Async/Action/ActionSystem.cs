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

        public void AddAction(IAction action)
        {
            ActionList_.Add(action);
            action.Execute();
        }

        public void AddBuilder(ActionBuilder builder)
        {
            AddAction(builder.Flush());
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