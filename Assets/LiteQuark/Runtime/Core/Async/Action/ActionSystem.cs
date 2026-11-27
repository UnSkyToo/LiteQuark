using System.Threading;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    [LiteHideType]
    public sealed class ActionSystem : ISystem, ITick
    {
        private readonly SafeList<BaseAction> _actionList = new();

        public ActionSystem()
        {
        }

        public UniTask<bool> Initialize(CancellationToken ct = default)
        {
            return UniTask.FromResult(true);
        }

        public UniTask DisposeAsync()
        {
            foreach (var action in _actionList)
            {
                action.Cancel();
                action.Dispose();
            }

            _actionList.Clear();
            return UniTask.CompletedTask;
        }

        public void Tick(float deltaTime)
        {
            var unscaledDeltaTime = LiteTime.UnscaledDeltaTime;

            foreach (var action in _actionList)
            {
                try
                {
                    if (!action.IsDone)
                    {
                        action.Tick(action.IsUnscaled ? unscaledDeltaTime : deltaTime);
                    }

                    if (action.IsDone)
                    {
                        _actionList.Remove(action);
                        action.Dispose();
                    }
                }
                catch
                {
                    action.Cancel();
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

        internal SafeList<BaseAction> GetActionList()
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
            FindAction(id)?.Cancel();
        }

        public IAction AddAction(BaseAction action, bool isSafety = false)
        {
            if (isSafety || LiteRuntime.Setting.Action.SafetyMode)
            {
                action.MarkAsSafe();
            }

            _actionList.Add(action);
            action.Execute();
            return action;
        }

        public IAction AddBuilder(ActionBuilder builder, bool isSafety = false)
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
