using System;
using System.Collections;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Editor
{
    // Local-only harness against the unmodified production source snapshot.
    internal static partial class CorePlayModeValidationRunner
    {
        private const string PendingKey = "LiteQuark.CorePlayModeValidation.Pending";
        private const string ReportKey = "LiteQuark.CorePlayModeValidation.Report";
        private const string LauncherSettingsResultKey = "LiteQuark.CorePlayModeValidation.LauncherSettings";
        private const string EditorStopStageKey = "LiteQuark.CorePlayModeValidation.EditorStopStage";
        private const string EditorStopResultKey = "LiteQuark.CorePlayModeValidation.EditorStopResult";
        private const string EditorStopErrorsKey = "LiteQuark.CorePlayModeValidation.EditorStopErrors";
        private const string EditorStopLogOffsetKey = "LiteQuark.CorePlayModeValidation.EditorStopLogOffset";
        private static double _editorStopFinishAt;
        private static readonly ValidationReport Results = new();
        private static bool _running;
        private static ValidationCase _current;

        public static void Begin()
        {
            BundleValidation.BuildFixtures("tasks");
            SessionState.SetString(LauncherSettingsResultKey, JsonUtility.ToJson(LauncherSettingsValidation.RunInEditMode()));
            SessionState.SetString(ReportKey, Path.Combine(ValidationEnvironment.Reports("tasks"), "results.json"));
            SessionState.SetInt(EditorStopStageKey, 1);
            SessionState.SetString(EditorStopResultKey, string.Empty);
            SessionState.SetString(EditorStopErrorsKey, string.Empty);
            SessionState.SetString(EditorStopLogOffsetKey, new FileInfo(Application.consoleLogPath).Length.ToString());
            EditorSceneManager.OpenScene("Assets/Scenes/Demo.unity", OpenSceneMode.Single);
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            Application.logMessageReceived += CaptureEditorStopError;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += StartIfPending;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) StartIfPending();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(EditorStopStageKey, 0) == 2)
            {
                _editorStopFinishAt = EditorApplication.timeSinceStartup + 0.25;
                EditorApplication.update += FinishEditorStop;
            }
        }

        private static void StartIfPending()
        {
            if (_running || !EditorApplication.isPlaying || !SessionState.GetBool(PendingKey, false)) return;
            _running = true;
            SessionState.SetBool(PendingKey, false);
            if (SessionState.GetInt(EditorStopStageKey, 0) == 1)
            {
                StopDemoPlayMode().Forget();
                return;
            }
            RunAll().Forget();
        }

        private static async UniTask StopDemoPlayMode()
        {
            var started = Time.realtimeSinceStartup;
            var result = new ValidationCase { id = "tasks/demo-stop" };
            try
            {
                var deadline = Time.realtimeSinceStartup + 20f;
                while (LiteRuntime.Instance.State != LiteRuntime.RuntimeState.Running)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Demo startup before Editor stop");
                    await UniTask.NextFrame();
                }
                result.checks++;
                var launcher = LiteRuntime.Instance.Launcher;
                var sceneUI = (UISystemSetting)launcher.Settings.SystemList.Single().Setting;
                if (sceneUI.UICamera == null || LiteRuntime.Get<UISystem>().UICamera != sceneUI.UICamera ||
                    sceneUI.ResolutionWidth != 1080 || sceneUI.ResolutionHeight != 1920 || !DemoProfilesMatch(launcher))
                    throw new Exception("Demo must use its scene UI registration and saved Release/Development settings.");
                if (!string.IsNullOrEmpty(LiteRuntime.Setting.GameEntry))
                    throw new Exception("The default Demo must run without a game entry.");
                var viewer = GameObject.Find("IngameDebugConsole(Clone)");
                var canvas = LiteRuntime.Get<UISystem>().CanvasRoot;
                if (viewer == null || viewer.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution !=
                    canvas.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution)
                    throw new Exception("Demo must create its log viewer with the initialized UI canvas resolution.");
                result.evidence.Add("The default Demo reaches Running without a game entry and its log viewer matches the UI canvas.");
                var task = LiteRuntime.Task.AddTask(WaitUntilEditorStop());
                while (!task.IsExecuted)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Demo coroutine before Editor stop");
                    await UniTask.NextFrame();
                }
                result.checks++;
                result.evidence.Add("A real scheduled coroutine is running on the Launcher when the Editor stops Play Mode.");
            }
            catch (Exception ex) { result.failure = ex.ToString(); }
            finally
            {
                result.seconds = Time.realtimeSinceStartup - started;
                SessionState.SetString(EditorStopResultKey, JsonUtility.ToJson(result));
                SessionState.SetInt(EditorStopStageKey, 2);
                // Exercise Unity's actual quit/destroy order, not a prior awaited Runtime.Shutdown.
                EditorApplication.ExitPlaymode();
            }
        }

        private static IEnumerator WaitUntilEditorStop()
        {
            while (true) yield return null;
        }

        private static void CaptureEditorStopError(string message, string stack, LogType type)
        {
            if (SessionState.GetInt(EditorStopStageKey, 0) == 0 || !ValidationCaseExecution.IsError(type)) return;
            SessionState.SetString(EditorStopErrorsKey, SessionState.GetString(EditorStopErrorsKey, string.Empty) + message + "\n" + stack + "\n");
        }

        private static void FinishEditorStop()
        {
            if (EditorApplication.timeSinceStartup < _editorStopFinishAt) return;
            EditorApplication.update -= FinishEditorStop;
            var result = JsonUtility.FromJson<ValidationCase>(SessionState.GetString(EditorStopResultKey, string.Empty));
            var errors = SessionState.GetString(EditorStopErrorsKey, string.Empty);
            // Unity may reset managed log callbacks at the mode boundary; check the same interval in its native log too.
            using (var stream = new FileStream(Application.consoleLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(long.Parse(SessionState.GetString(EditorStopLogOffsetKey, "0")), SeekOrigin.Begin);
                using var reader = new StreamReader(stream);
                var log = reader.ReadToEnd();
                if (log.Contains("Destroy may not be called from edit mode") || log.Contains("MissingReferenceException"))
                    errors += "Native Editor log contains an invalid destruction or destroyed-object access during Demo stop.\n";
            }
            result.checks++;
            if (EditorApplication.isPlaying || errors.Length > 0)
                result.failure += "\nEditor stop did not complete without errors:\n" + errors;
            else
                result.evidence.Add("Actual Play Mode exit and edit-mode continuation drain produced no errors.");
            result.passed = string.IsNullOrEmpty(result.failure);
            SessionState.SetString(EditorStopResultKey, JsonUtility.ToJson(result));
            SessionState.SetInt(EditorStopStageKey, 0);
            _running = false;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static async UniTask RunAll()
        {
            Results.unityVersion = Application.unityVersion;
            Application.logMessageReceived += CaptureOutsideCaseError;
            try
            {
                const string battleScene = "Assets/LiteBattle/Resources/Scenes/LiteNexusEditorScene.unity";
                await EditorSceneManager.LoadSceneAsyncInPlayMode(battleScene, new LoadSceneParameters(LoadSceneMode.Additive)).ToUniTask();
                await RunCaseAsync("tasks/battle-entry", _ => BattleGameEntry());
                await LiteRuntime.Instance.Shutdown();
                UnityEngine.Object.Destroy(LiteRuntime.Instance.Launcher.gameObject);
                await SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(battleScene)).ToUniTask();

                var launcher = new GameObject("CorePlayModeValidationLauncher").AddComponent<LiteLauncher>();
                var deadline = Time.realtimeSinceStartup + 20f;
                while (LiteRuntime.Instance.State != LiteRuntime.RuntimeState.Running)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new Exception("Runtime startup timed out");
                    await UniTask.Yield();
                }
                // Keep the real coroutine host, but manually tick each isolated TaskSystem.
                launcher.enabled = false;

                await RunTaskCase("tasks/admission", PriorityOrder);
                await RunTaskCaseAsync("tasks/cancel", CancelQueue);
                await RunTaskCase("tasks/terminal", TerminalContract, "Validation execution failure");
                await RunTaskCaseAsync("tasks/coroutine-child", FlowParent);
                await RunTaskCaseAsync("tasks/download-result", WebGetSnapshots, "task-snapshot-missing.bin");
                await RunTaskCaseAsync("tasks/download-retry", WebGetRetries, "UnityWebGetRequestTask failed:", "Enter <ErrorState>", "FrameworkError: Unknown");
                await RunCaseAsync("tasks/timer-control", TimerValidation.Exercise);
                await RunCaseAsync("tasks/action-cancel", ActionValidation.Exercise);
                await RunCaseAsync("tasks/event-scope", result =>
                {
                    EventValidation.Exercise(result);
                    return UniTask.CompletedTask;
                });
                await RunTaskCaseAsync("tasks/instantiate", InstantiationResults, "The Object you want to instantiate is null");
                launcher.enabled = true;
                try { await RunCaseAsync("tasks/pool-ownership", PoolValidation.Exercise); }
                finally { launcher.enabled = false; }
                await RunCaseAsync("tasks/startup-rollback", async _ =>
                {
                    await RuntimeInitializationReturnsFalse();
                    await RuntimeInitializationThrows();
                    await RuntimeShutdownDuringInitialization();
                }, expectedErrors: new[]
                {
                    $"Initialize {typeof(ShutdownSystemTwo).AssemblyQualifiedName} failed",
                    "InjectedValidation runtime initialization", "Enter <ErrorState>", "FrameworkError: Startup"
                });
                await RunCaseAsync("tasks/ordered-shutdown", _ => RuntimeShutdownOrder(), expectedErrors: new[] { "FrameworkError: Unknown" });
                await RunCaseAsync("tasks/settings-profiles", _ => RuntimeSettingsProfiles(), expectedErrors: new[] { LogProbePrefix });
                await RunCaseAsync("tasks/late-tick", _ => RuntimeLateTick());
                await RunCaseAsync("tasks/restart", async _ =>
                {
                    await RuntimeRestartStates();
                    await RuntimeRestartRequests();
                }, expectedErrors: new[]
                {
                    "Enter <ErrorState>", "FrameworkError: Startup", "FrameworkError: Unknown",
                    "Cannot restart runtime in state Error.", $"Initialize {typeof(ShutdownSystemTwo).AssemblyQualifiedName} failed"
                });
                await RunCaseAsync("tasks/shutdown-failure", _ => RuntimeShutdownFailure(), expectedErrors: new[] { "InjectedValidation partial system disposal" });
            }
            catch (Exception ex)
            {
                Results.fatalError = ex.ToString();
            }
            finally
            {
                Application.logMessageReceived -= CaptureOutsideCaseError;
                var editorStop = JsonUtility.FromJson<ValidationCase>(SessionState.GetString(EditorStopResultKey, string.Empty));
                Results.cases.Add(editorStop);
                Results.cases.Add(JsonUtility.FromJson<ValidationCase>(SessionState.GetString(LauncherSettingsResultKey, string.Empty)));
                Results.Finish("tasks");
            }
        }

        private static async UniTask BattleGameEntry()
        {
            await UniTask.WaitUntil(() => LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Running)
                .Timeout(TimeSpan.FromSeconds(20));
            var launcher = LiteRuntime.Instance.Launcher;
            var sceneUI = launcher.Settings.SystemList.Select(entry => entry.Setting).OfType<UISystemSetting>().Single();
            Check(!string.IsNullOrEmpty(launcher.Settings.GameEntry) && LiteRuntime.Setting.GameEntry == launcher.Settings.GameEntry &&
                sceneUI.UICamera != null && LiteRuntime.Get<UISystem>().UICamera == sceneUI.UICamera && sceneUI.UICamera != Camera.main,
                "Battle uses its scene entry and the dedicated camera stored on its UI registration.");
            Check(DemoProfilesMatch(launcher) && sceneUI.ResolutionWidth == 1080 && sceneUI.ResolutionHeight == 1920,
                "Battle keeps the Release defaults and applies its Development resource, CDN and log overrides.");

            var camera = Camera.main;
            var ui = LiteRuntime.Get<UISystem>();
            var layer = (RectTransform)ui.CanvasRoot.Find("Canvas_Scene");
            await UniTask.WaitUntil(() => layer.childCount == 2).Timeout(TimeSpan.FromSeconds(20));
            await UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate);
            var monster = GameObject.Find("FemaleCharacter(Clone)").transform;
            Vector2 ProjectMonster()
            {
                var pixel = camera.WorldToScreenPoint(monster.position + Vector3.up * 2f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, pixel, ui.UICamera, out var point);
                return point;
            }
            var before = ProjectMonster();
            var plate = layer.Cast<RectTransform>().Single(rt => Vector2.Distance(rt.anchoredPosition, before) < 0.5f);

            // The isolated Editor assembly reaches the battle example in Assembly-CSharp through its public API.
            var managerType = Type.GetType("LiteBattle.Runtime.LiteEntityManager, Assembly-CSharp", true);
            var manager = managerType.BaseType.GetProperty("Instance").GetValue(null);
            var units = (IList)managerType.GetMethod("GetEntityList").Invoke(manager, null);
            var player = units.Cast<object>().Single(unit => (string)unit.GetType().GetProperty("Tag").GetValue(unit) == "Player");
            var position = player.GetType().GetProperty("Position");
            var shift = Vector3.right * 4f;
            var cameraBefore = camera.transform.position;
            position.SetValue(player, (Vector3)position.GetValue(player) + shift);

            await UniTask.NextFrame(PlayerLoopTiming.PreLateUpdate);
            var frame = Time.frameCount;
            Check(camera.transform.position == cameraBefore, "Ordinary Update leaves battle camera follow for LateUpdate.");
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            var expected = ProjectMonster();
            Check(Time.frameCount == frame && camera.transform.position == cameraBefore + shift &&
                Vector2.Distance(before, expected) > 1f && Vector2.Distance(plate.anchoredPosition, expected) < 0.5f,
                "The same frame follows the moving player and projects the stationary monster's plate using the final camera pose.");
        }

        private static bool DemoProfilesMatch(LiteLauncher launcher)
        {
            var release = launcher.Settings.CreateRuntimeSetting(LiteBuildProfile.Release);
            var active = LiteRuntime.Setting;
            return release.Asset.BundleMode == BundleLoadMode.BuiltIn && release.Asset.BundleRemoteUri == string.Empty &&
                release.Log.Levels == (LogLevel.Error | LogLevel.Fatal) && !release.Log.ShowLogViewer &&
                active.Environment == LiteBuildProfile.Development && active.Asset.BundleMode == BundleLoadMode.Simulate &&
                active.Asset.BundleRemoteUri == "https://localhost:8000/" && active.Log.Levels == LogLevel.All && active.Log.ShowLogViewer;
        }

        private static async UniTask RunCaseAsync(string name, Func<ValidationCase, UniTask> test,
            Func<UniTask> cleanup = null, params string[] expectedErrors)
        {
            using (var execution = new ValidationCaseExecution(Results, name, RecordError))
            {
                _current = execution.Result;
                await execution.RunAsync(() => test(execution.Result), cleanup ?? (() => UniTask.CompletedTask));
                execution.RejectUnexpectedErrors();
            }
            Debug.Log($"CORE_PLAY_MODE_CASE: {name}: {(_current.passed ? "PASS" : "FAIL")}");
            _current = null;

            void RecordError(ValidationCase item, string message, string stack)
            {
                if (expectedErrors.Any(message.Contains))
                {
                    // Lifecycle assertions read this evidence before the case finishes.
                    item.evidence.Add("Expected error log: " + message);
                    return;
                }
                item.errors.Add(message);
                Results.unexpectedErrors.Add(message + "\n" + stack);
            }
        }

        private static void Check(bool condition, string evidence)
        {
            _current.Check(condition, evidence);
        }

        private static void Throws<T>(Action action, string evidence) where T : Exception
        {
            var thrown = false;
            try { action(); } catch (T) { thrown = true; }
            Check(thrown, evidence);
        }

        private static void CaptureOutsideCaseError(string message, string stack, LogType type)
        {
            if (_current == null && ValidationCaseExecution.IsError(type))
                Results.unexpectedErrors.Add(message + "\n" + stack);
        }
    }
}
