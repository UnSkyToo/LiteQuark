using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed partial class AddonsValidationRunner : BundleValidation
    {
        private sealed class CloseEvent : IEventData { }

        public sealed class ProbeUI : BaseUI
        {
            internal static Action<ProbeUI> Opening;
            internal Action Updating;
            internal Action Closing;
            internal void Listen(Action callback) => RegisterEvent<CloseEvent>(_ => callback());
            protected override void OnOpen(params object[] values) => Opening?.Invoke(this);
            protected override void OnUpdate(float deltaTime) => Updating?.Invoke();
            protected override void OnClose() => Closing?.Invoke();
        }

        private static UniTask<ProbeUI> Open(UISystem ui, CancellationToken token = default)
            => ui.OpenUI<ProbeUI>(new UIConfig(PrefabPath, UIDepthMode.Normal, false, false), token);

        private static async UniTask UIOpen()
        {
            using var content = new AssetScope();
            var camera = new GameObject("Validation UI camera", typeof(Camera));
            var ui = new UISystem { Setting = new UISystemSetting { UICamera = camera.GetComponent<Camera>() } };
            var owner = NewUIOwnerEntity();
            var opens = 0;
            ProbeUI.Opening = _ => opens++;
            try
            {
                await ui.Initialize();
                var first = Open(ui, owner.Token);
                var second = Open(ui);
                owner.Entity.Dispose();
                var cancelled = false;
                try { await first; } catch (OperationCanceledException ex) { cancelled = ex.CancellationToken == owner.Token; }
                Check(cancelled && opens == 0, "Disposed caller receives cancellation before prefab delivery.");
                await Wait(() => second.Status.IsCompleted(), "Shared UI handoff");
                var window = await second;
                Check(window.Go != null && opens == 1, "Independent open on the same prefab survives.");
                using var handoff = new CancellationTokenSource();
                GameObject abandoned = null;
                ProbeUI.Opening = opened =>
                {
                    abandoned = opened.Go;
                    handoff.Cancel();
                    handoff.Token.ThrowIfCancellationRequested();
                };
                var interrupted = Open(ui, handoff.Token);
                await Wait(() => interrupted.Status.IsCompleted(), "Cancellation inside OnOpen");
                cancelled = false;
                try { await interrupted; } catch (OperationCanceledException) { cancelled = true; }
                await Wait(() => abandoned == null, "Unpublished instance destruction");
                Check(cancelled && window.Go != null, "Cancellation at handoff releases just the unpublished UI.");
            }
            finally
            {
                ProbeUI.Opening = null;
                owner.Entity.Dispose();
                await ui.DisposeAsync();
                UnityEngine.Object.Destroy(camera);
            }
        }

        private static async UniTask UIClose()
        {
            using var content = new AssetScope();
            var camera = new GameObject("Validation UI camera", typeof(Camera));
            var ui = new UISystem { Setting = new UISystemSetting { UICamera = camera.GetComponent<Camera>() } };
            try
            {
                await ui.Initialize();
                var opening = Open(ui);
                await Wait(() => opening.Status.IsCompleted(), "First UI");
                var first = await opening;
                opening = Open(ui);
                await Wait(() => opening.Status.IsCompleted(), "Second UI");
                var second = await opening;
                int updates = 0, closes = 0, events = 0;
                second.Listen(() => events++);
                LiteRuntime.Event.Send(new CloseEvent());
                Check(events == 1, "Second UI owns a live event listener.");
                first.Updating = () => ui.CloseUI(second);
                second.Updating = () => updates++;
                second.Closing = () => { closes++; ui.CloseUI(second); };
                ui.Tick(0);
                Check(updates == 0 && closes == 1 && second.Go != null, "Closed sibling skips the current update but retains its instance until the next tick.");
                ui.Tick(0);
                LiteRuntime.Event.Send(new CloseEvent());
                Check(second.Go == null && closes == 1 && events == 1, "Close queue releases the instance and its tagged listener exactly once.");
                var root = ui.CanvasRoot;
                first.Closing = () => throw new InvalidOperationException("Validation OnClose");
                await ui.DisposeAsync();
                await Wait(() => root == null, "Actual Canvas destruction");
                Check(first.Go == null && ui.CanvasRoot == null, "Business close failure still releases the Canvas and the remaining UI.");
            }
            finally { await ui.DisposeAsync(); UnityEngine.Object.Destroy(camera); }
        }

        private static (IDisposable Entity, CancellationToken Token) NewUIOwnerEntity()
        {
            // Construct the real entity without starting battle logic or adding an assembly reference.
            var type = Type.GetType("LiteBattle.Runtime.LiteUnit, Assembly-CSharp", true);
            var entity = (IDisposable)Activator.CreateInstance(type, new object[] { null });
            return (entity, (CancellationToken)type.GetProperty("LifetimeToken").GetValue(entity));
        }
    }
}
