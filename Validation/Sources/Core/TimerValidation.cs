using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;

namespace LiteQuark.Editor
{
    internal static class TimerValidation
    {
        internal static async UniTask Exercise(ValidationCase result)
        {
            var timers = new TimerSystem();
            await timers.Initialize();
            try
            {
                var ticks = 0;
                var completions = 0;
                ITimer timer = timers.AddTimer(1f, () => ticks++, () => completions++, totalTime: 1f);
                result.Check(ReferenceEquals(timers.FindTimer(timer.ID), timer), "Creation returns the registered timer.");

                timer.Pause();
                timers.Tick(1f);
                result.Check(timer.IsPaused && !timer.IsDone && ticks == 0, "The returned timer can pause its own updates.");
                timer.Resume();
                timers.Tick(1f);
                timer.Cancel();
                result.Check(timer.IsDone && ticks == 1 && completions == 1 && timers.FindTimer(timer.ID) == null,
                    "Resuming allows natural completion and late cancellation leaves it intact.");

                ITimer cancelled = null;
                timers.NextFrame(() =>
                {
                    cancelled = timers.AddTimer(1f, () => ticks++, () => completions++, totalTime: 1f);
                    cancelled.Cancel();
                    cancelled.Cancel();
                });
                timers.Tick(0f);
                result.Check(cancelled is { IsDone: true }, "A timer created during a callback can be cancelled immediately by reference.");
                timers.Tick(1f);
                result.Check(ticks == 1 && completions == 1 && timers.FindTimer(cancelled.ID) == null,
                    "The cancelled timer sends no callbacks and is removed by the next tick.");
            }
            finally
            {
                await timers.DisposeAsync();
            }
        }
    }
}
