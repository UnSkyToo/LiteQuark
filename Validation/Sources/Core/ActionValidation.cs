using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;

namespace LiteQuark.Editor
{
    internal static class ActionValidation
    {
        internal static async UniTask Exercise(ValidationCase result)
        {
            var actions = new ActionSystem();
            await actions.Initialize();
            try
            {
                IAction action = null;
                var loopStarts = 0;
                var siblingCalls = 0;
                var doneInCallback = false;
                var builder = ActionBuilder.Parallel(repeatCount: -1)
                    .Callback(() => loopStarts++)
                    .BeginSequence()
                        .WaitFrame(1)
                        .Callback(() =>
                        {
                            action.Cancel();
                            doneInCallback = action.IsDone;
                        })
                    .EndSequence()
                    .DelayCallback(1f, () => siblingCalls++);
                action = actions.AddBuilder(builder);
                result.Check(loopStarts == 1 && !action.IsDone && ReferenceEquals(actions.FindAction(action.ID), action),
                    "Starting a parallel builder returns the registered action after the first loop has started.");

                actions.Tick(1f);
                result.Check(doneInCallback && action.IsDone && loopStarts == 1 && siblingCalls == 0,
                    "Cancelling the root from a child callback stops the current loop and prevents the next loop starting.");
                action.Cancel();
                actions.Tick(1f);
                result.Check(actions.IsIdle() && actions.FindAction(action.ID) == null && loopStarts == 1 && siblingCalls == 0,
                    "The scheduler removes the cancelled group and a second cancellation leaves it stopped.");

                var tailCalls = 0;
                var sequence = ActionBuilder.Sequence().WaitFrame(1).Callback(() => tailCalls++).Flush();
                IAction submitted = actions.AddAction(sequence);
                result.Check(ReferenceEquals(sequence, submitted), "Submitting a BaseAction returns the same action for control.");
                actions.Tick(0f);
                result.Check(submitted.IsDone && tailCalls == 1 && actions.IsIdle(),
                    "A naturally completed sequence still runs its tail callback and leaves the scheduler.");
            }
            finally
            {
                await actions.DisposeAsync();
            }
        }
    }
}
