using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Editor
{
    internal sealed partial class AssetLifecycleValidationRunner : BundleValidation
    {
        public static void Begin() => BeginSuite("lifecycle");

        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterPlayMode() => Resume("lifecycle", RunScenarios);

        private static async UniTask RunScenarios()
        {
            await Run("lifecycle/shared-retention", SharedRetention);
            await Run("lifecycle/preload", PreloadErrorProtocol, "Load asset failed");
            await Run("lifecycle/retry", SharedCancellationFailureRetry, "Load bundle failed");
            await Run("lifecycle/dependency-life", DependencyRetention);
            await Run("lifecycle/dependency-failure", DependencyFailure, "Load bundle failed");
            await Run("lifecycle/queued-cancel", QueuedCancel);
            await Run("lifecycle/submitted-asset", SubmittedAsset);
            await Run("lifecycle/late-bundle", ShutdownDuringLocalBundle);
            await Run("lifecycle/undelivered-bundle", ShutdownBeforeBundleDelivery);
            await Run("lifecycle/close-with-dependency", ShutdownWithPendingDependency, "Bundle load failed");
            await Run("lifecycle/remote-cleanup", RemoteCleanup);
            await Run("lifecycle/scene-instances", SceneInstances, "Scene is already loading");
            await Run("lifecycle/submitted-scene", async () =>
            {
                await ShutdownWaitsForNativeScene();
                await Cleanup();
                await EditorShutdownScene();
            });
            await Run("lifecycle/last-scene", ShutdownReportsSceneRefusal, "Start scene unload failed", "Unloading the last loaded scene");
            await Run("lifecycle/scene-retry", SceneMissingMetadata, "Can't get bundle info");
            await Run("lifecycle/priority", PrioritySharedDependencies);
            await Run("lifecycle/quotas", async () =>
            {
                await AssetGroupLoaderBudgets(1);
                await Cleanup();
                await AssetGroupLoaderBudgets(3);
            });
            await Run("lifecycle/prefab-handle", InstantiateFailureProtocol);
        }
    }
}
