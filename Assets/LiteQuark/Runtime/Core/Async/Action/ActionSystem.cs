using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    [LiteHideType]
    public sealed class ActionSystem : ISystem, ITick
    {
        private readonly SafeList<IAction> _actionList = new();

        public ActionSystem()
        {
        }

        public UniTask<bool> Initialize()
        {
            return UniTask.FromResult(true);
        }

        public void Dispose()
        {
            foreach (var action in _actionList)
            {
                action.Dispose();
            }
            _actionList.Clear();
        }

        public void Tick(float deltaTime)
        {
            var unscaledDeltaTime = LiteTime.UnscaledDeltaTime;

            foreach (var action in _actionList)
            {
                try
                {
                    if (action.IsDone)
                    {
                        LiteUtils.SafeInvoke(action.FinalCallback, action);
                        action.Dispose();
                        _actionList.Remove(action);
                    }
                    else
                    {
                        action.Tick(action.IsUnscaled ? unscaledDeltaTime : deltaTime);
                    }
                }
                catch
                {
                    action.Stop();
                    throw;
                }
            }
        }

        public bool IsIdle()
        {
            return _actionList.Count == 0;
        }

        public bool IsEnd(ulong id)
        {
            var action = FindAction(id);
            return action == null || action.IsDone;
        }

        public SafeList<IAction> GetActionList()
        {
            return _actionList;
        }

        public IAction FindAction(ulong id)
        {
            if (id == 0)
            {
                return null;
            }

            foreach (var action in _actionList)
            {
                if (action.ID == id)
                {
                    return action;
                }
            }

            return null;
        }

        public void StopAction(ulong id)
        {
            var action = FindAction(id);
            if (action == null || action.IsDone)
            {
                return;
            }
            action.Stop();

            try
            {
                LiteUtils.SafeInvoke(action.FinalCallback, action);
                action.Dispose();
            }
            finally
            {
                _actionList.Remove(action);
            }
        }

        public ulong AddAction(IAction action, bool isSafety = false)
        {
            if (isSafety || LiteRuntime.Setting.Action.SafetyMode)
            {
                action.MarkAsSafe();
            }

            _actionList.Add(action);
            action.Execute();
            return action.ID;
        }

        public ulong AddBuilder(ActionBuilder builder, bool isSafety = false)
        {
            return AddAction(builder.Flush(), isSafety);
        }

        public ActionBuilder Sequence(string tag, int repeatCount = 1)
        {
            return ActionBuilder.Sequence(tag, repeatCount);
        }

        public ActionBuilder Parallel(string tag, int repeatCount = 1)
        {
            return ActionBuilder.Parallel(tag, repeatCount);
        }
    }
}
