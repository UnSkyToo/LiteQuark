using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed partial class AddonsValidationRunner : BundleValidation
    {
        public static void Begin() => BeginSuite("addons", BuildMedia);

        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterPlayMode() => Resume("addons", RunScenarios);

        private static async UniTask RunScenarios()
        {
            await Run("addons/ui-open", UIOpen);
            await Run("addons/ui-mutex", UIMutex);
            await Run("addons/ui-close", UIClose, "Validation OnClose");
            await Run("addons/ui-sort", UISort);
            await Run("addons/ui-camera", UICameraFallback);
            await Run("addons/audio", Audio);
            await Run("addons/effect-pending", EffectShutdownPendingLoads);
            await Run("addons/effect-reuse", EffectTerminalReuse);
            await Run("addons/network-queue", NetworkQueue);
            await Run("addons/network-retry", NetworkRetry);
            await Run("addons/network-interceptor", NetworkInterceptor);
            await Run("addons/network-files", NetworkFiles);
            await Run("addons/data-io", DataIO);
            await Run("addons/data-switch", DataSwitch);
            await Run("addons/data-persistence", DataPersistence);
            await Run("addons/config-reload", ConfigReload, "ConfigSystem: error loading");
            await Run("addons/config-formats", ConfigFormats);
            await Run("addons/config-close", ConfigClose);
            await Run("addons/restart-ui", RuntimeRestartPendingUI, "Enter <ErrorState>", "FrameworkError: Unknown, Framework error");
        }

        private sealed class AssetScope : IDisposable
        {
            private readonly AssetManager _previous = Get<AssetManager>(LiteRuntime.Asset, "_manager");
            internal readonly Harness Content;

            internal AssetScope(bool manual = false)
            {
                Content = New(manual);
                Set(LiteRuntime.Asset, "_manager", Content.Provider);
            }
            public void Dispose() => Set(LiteRuntime.Asset, "_manager", _previous);
        }
    }
}
