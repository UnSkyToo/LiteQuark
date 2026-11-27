using System;
using LiteQuark.Runtime;

namespace LiteQuark.Editor
{
    internal static class EventValidation
    {
        private struct ScopedEvent : IEventData { }
        private struct OtherScopedEvent : IEventData { }

        internal static void Exercise(ValidationCase result)
        {
            var events = LiteRuntime.Event;
            using var first = events.CreateSubscriptionScope();
            using var second = events.CreateSubscriptionScope();
            var sharedCalls = 0;
            var otherCalls = 0;
            Action<ScopedEvent> shared = message => sharedCalls++;
            events.Register(shared);
            try
            {
                first.Register(shared);
                first.Register(shared);
                second.Register(shared);
                first.Register<OtherScopedEvent>(message => otherCalls++);
                events.Send<ScopedEvent>();
                events.Send<OtherScopedEvent>();
                result.Check(sharedCalls == 3 && otherCalls == 1,
                    "Each owner receives its own subscription and duplicate registration in one scope is ignored.");

                first.Dispose();
                first.Dispose();
                events.Send<ScopedEvent>();
                events.Send<OtherScopedEvent>();
                result.Check(sharedCalls == 5 && otherCalls == 1,
                    "Disposing a scope removes its subscriptions across event types and leaves the other scope and default group intact.");

                var remainingCalls = 0;
                second.Register<ScopedEvent>(message => remainingCalls++);
                second.UnRegister(shared);
                events.Send<ScopedEvent>();
                result.Check(sharedCalls == 6 && remainingCalls == 1,
                    "Unregistering one callback preserves the rest of its scope and the matching default subscription.");

                using var snapshotScope = events.CreateSubscriptionScope();
                var tailCalls = 0;
                snapshotScope.Register<ScopedEvent>(message => snapshotScope.Dispose());
                snapshotScope.Register<ScopedEvent>(message => tailCalls++);
                events.Send<ScopedEvent>();
                result.Check(tailCalls == 1, "Disposal during a callback preserves the rest of the current send snapshot.");
                events.Send<ScopedEvent>();
                result.Check(tailCalls == 1, "Disposal during a callback takes effect on the next send.");
            }
            finally
            {
                events.UnRegister(shared);
            }
        }
    }
}
