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

        public sealed class ProbeUI : BaseUI, ILateTick
        {
            internal static Action<ProbeUI> Opening;
            internal Action Updating;
            internal Action LateUpdating;
            internal Action Closing;
            internal void Listen(Action callback) => RegisterEvent<CloseEvent>(_ => callback());
            protected override void OnOpen(params object[] values) => Opening?.Invoke(this);
            protected override void OnUpdate(float deltaTime) => Updating?.Invoke();
            public void LateTick(float deltaTime) => LateUpdating?.Invoke();
            protected override void OnClose() => Closing?.Invoke();
        }

        private static UniTask<ProbeUI> Open(UISystem ui, CancellationToken token = default, UIDepthMode depthMode = UIDepthMode.Normal)
            => ui.OpenUI<ProbeUI>(new UIConfig(PrefabPath, depthMode, false, false), token);

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
                var closes = 0;
                ProbeUI.Opening = opened =>
                {
                    opened.Closing = () => closes++;
                    handoff.Cancel();
                };
                var delivered = await Open(ui, handoff.Token);
                Check(handoff.IsCancellationRequested && delivered.Go != null && delivered.State == UIState.Opened && closes == 0,
                    "A normally returned OnOpen delivers its window even when the callback cancels the token.");
                ui.CloseUI(delivered);
                ui.Tick(0);
                Check(closes == 1 && delivered.Go == null && window.Go != null,
                    "Closing the delivered window runs its business cleanup once and releases its instance.");
            }
            finally
            {
                ProbeUI.Opening = null;
                owner.Entity.Dispose();
                await ui.DisposeAsync();
                UnityEngine.Object.Destroy(camera);
            }
        }

        private static async UniTask UIMutex()
        {
            using var content = new AssetScope();
            var camera = new GameObject("Validation UI camera", typeof(Camera));
            var ui = new UISystem { Setting = new UISystemSetting { UICamera = camera.GetComponent<Camera>() } };
            var config = new UIConfig(PrefabPath, UIDepthMode.Normal, autoAdapt: false);
            var laterConfig = new UIConfig(PrefabPath, UIDepthMode.Top, autoAdapt: false);
            var opens = 0;
            var visibleWhileOpening = false;
            ProbeUI.Opening = window =>
            {
                opens++;
                visibleWhileOpening |= ReferenceEquals(window, ui.FindUI<ProbeUI>());
            };
            try
            {
                await ui.Initialize();
                using var owner = new CancellationTokenSource();
                var abandoned = ui.OpenUI<ProbeUI>(config, owner.Token);
                var sharedAbandoned = ui.OpenUI<ProbeUI>(config);
                Check(!abandoned.Status.IsCompleted() && !sharedAbandoned.Status.IsCompleted() && ui.FindUI<ProbeUI>() == null,
                    "An in-flight mutex open is shared and remains undiscoverable until handoff.");
                owner.Cancel();
                Check(await WasCanceled(abandoned, owner.Token) && await WasCanceled(sharedAbandoned, owner.Token) && opens == 0,
                    "Canceling the owner cancels the shared open before any window opens.");

                using var observer = new CancellationTokenSource();
                var first = ui.OpenUI<ProbeUI>(config);
                var second = ui.OpenUI<ProbeUI>(laterConfig);
                var canceledWait = ui.OpenUI<ProbeUI>(config, observer.Token);
                observer.Cancel();
                Check(await WasCanceled(canceledWait, observer.Token), "A joined caller can cancel its own wait.");
                var window = await first;
                Check(window != null && ReferenceEquals(window, await second) && opens == 1 &&
                    window.Config.DepthMode == UIDepthMode.Normal,
                    "Overlapping mutex opens return the same instance using the first request's config.");
                Check(!visibleWhileOpening && ReferenceEquals(window, ui.FindUI<ProbeUI>()) &&
                    ReferenceEquals(window, await ui.OpenUI<ProbeUI>(config)),
                    "A window can be found and reused after OnOpen finishes, before the next UI tick.");

                ui.CloseUI<ProbeUI>();
                Check(window.State == UIState.Closing && ui.FindUI<ProbeUI>() == null,
                    "Closing by type works in the same frame and the closing window is no longer discoverable.");
                var reopened = await ui.OpenUI<ProbeUI>(config);
                Check(reopened != null && !ReferenceEquals(reopened, window) && opens == 2,
                    "A closing window leaves room for the next mutex open.");
                ui.CloseAllUI();
                ui.Tick(0);
                Check(window.Go == null && reopened.Go == null, "CloseAllUI includes windows opened before the next tick.");
            }
            finally
            {
                ProbeUI.Opening = null;
                await ui.DisposeAsync();
                UnityEngine.Object.Destroy(camera);
            }
        }

        private static async UniTask<bool> WasCanceled(UniTask<ProbeUI> opening, CancellationToken token)
        {
            try { await opening; }
            catch (OperationCanceledException ex) { return ex.CancellationToken == token; }
            return false;
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
                var lateUpdates = 0;
                second.LateUpdating = () => lateUpdates++;
                ui.LateTick(0);
                Check(lateUpdates == 1 && ReferenceEquals(ui.FindUI<ProbeUI>(), first),
                    "An opened window can project and be found before its first Update.");
                second.Listen(() => events++);
                LiteRuntime.Event.Send(new CloseEvent());
                Check(events == 1, "Second UI owns a live event listener.");
                first.Updating = () => ui.CloseUI(second);
                second.Updating = () => updates++;
                second.Closing = () => { closes++; ui.CloseUI(second); };
                ui.Tick(0);
                Check(updates == 0 && closes == 1 && second.Go != null, "Closed sibling skips the current update but retains its instance until the next tick.");
                ui.LateTick(0);
                Check(lateUpdates == 1 && second.Go != null,
                    "A window closed during Update skips LateTick and its close queue still waits for Update.");
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

        private static async UniTask UISort()
        {
            using var content = new AssetScope();
            var camera = new GameObject("Validation UI camera", typeof(Camera));
            var ui = new UISystem { Setting = new UISystemSetting { UICamera = camera.GetComponent<Camera>() } };
            try
            {
                await ui.Initialize();
                var first = await Open(ui);
                var middle = await Open(ui);
                var last = await Open(ui);
                var top = await Open(ui, depthMode: UIDepthMode.Top);
                var sceneFirst = await Open(ui, depthMode: UIDepthMode.Scene);
                var sceneSecond = await Open(ui, depthMode: UIDepthMode.Scene);
                Check(HasOrder(first, 10000) && HasOrder(middle, 10100) && HasOrder(last, 10200),
                    "Windows opened before the first UI tick are ordered within their layer.");

                ui.CloseUI(middle);
                var next = await Open(ui);
                Check(HasOrder(middle, 10100) && HasOrder(next, 10300),
                    "Opening while a sibling is still closing keeps both canvases in order.");

                ui.Tick(0);
                Check(middle.Go == null && HasOrder(first, 10000) && HasOrder(last, 10100) && HasOrder(next, 10200),
                    "Releasing the middle window compacts its layer and updates the public sorting orders.");
                var reopened = await Open(ui);
                Check(HasOrder(reopened, 10300) && HasOrder(top, 15000) &&
                    HasOrder(sceneFirst, 0) && HasOrder(sceneSecond, 0),
                    "The next window stacks above its layer without affecting Top or Scene.");

                UniTask<ProbeUI> nested = default;
                ProbeUI.Opening = _ =>
                {
                    ProbeUI.Opening = null;
                    nested = Open(ui);
                };
                var outer = await Open(ui);
                Check(nested.Status == UniTaskStatus.Succeeded, "A cached same-layer window can open synchronously inside OnOpen.");
                var inner = await nested;
                Check(HasOrder(inner, 10400) && HasOrder(outer, 10500),
                    "Both windows have distinct sorting orders in publication order.");
            }
            finally
            {
                ProbeUI.Opening = null;
                await ui.DisposeAsync();
                UnityEngine.Object.Destroy(camera);
            }
        }

        private static bool HasOrder(BaseUI ui, int order)
            => ui.SortingOrder == order && ui.Go.GetComponent<Canvas>().sortingOrder == order;

        private static async UniTask UICameraFallback()
        {
            var cameraObject = new GameObject("Validation perspective main camera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.orthographic = false;
            camera.orthographicSize = 7f;
            var fallback = new UISystem { Setting = new UISystemSetting() };
            var overlay = new UISystem { Setting = new UISystemSetting() };
            try
            {
                await fallback.Initialize();
                var canvas = fallback.CanvasRoot.GetComponent<Canvas>();
                Check(fallback.UICamera == camera && canvas.worldCamera == camera && canvas.renderMode == RenderMode.ScreenSpaceCamera &&
                    !camera.orthographic && camera.orthographicSize == 7f,
                    "The unconfigured UI uses Camera.main without adjusting its projection or size.");

                cameraObject.tag = "Untagged";
                await overlay.Initialize();
                canvas = overlay.CanvasRoot.GetComponent<Canvas>();
                Check(overlay.UICamera == null && canvas.worldCamera == null && canvas.renderMode == RenderMode.ScreenSpaceOverlay,
                    "With no configured or main camera, UI initializes in overlay mode.");
            }
            finally
            {
                await overlay.DisposeAsync();
                await fallback.DisposeAsync();
                UnityEngine.Object.Destroy(cameraObject);
            }
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
