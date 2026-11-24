using System;
using System.Collections;
using System.Collections.Generic;
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
    internal static partial class TaskSystemValidationRunner
    {
        private const string PendingKey = "LiteQuark.TaskSystemValidation.Pending";
        private const string ReportKey = "LiteQuark.TaskSystemValidation.Report";
        private const string LauncherSettingsResultKey = "LiteQuark.TaskSystemValidation.LauncherSettings";
        private const string EditorStopStageKey = "LiteQuark.TaskSystemValidation.EditorStopStage";
        private const string EditorStopResultKey = "LiteQuark.TaskSystemValidation.EditorStopResult";
        private const string EditorStopErrorsKey = "LiteQuark.TaskSystemValidation.EditorStopErrors";
        private const string EditorStopLogOffsetKey = "LiteQuark.TaskSystemValidation.EditorStopLogOffset";
        private static double _editorStopFinishAt;
        private static readonly ValidationReport Results = new();
        private static bool _running;
        private static ValidationCase _current;
        private static string[] _expectedErrors = Array.Empty<string>();

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
            if (SessionState.GetInt(EditorStopStageKey, 0) == 0 || type is not (LogType.Error or LogType.Exception or LogType.Assert)) return;
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
            Application.logMessageReceived += CaptureError;
            try
            {
                const string battleScene = "Assets/LiteBattle/Resources/Scenes/LiteNexusEditorScene.unity";
                await EditorSceneManager.LoadSceneAsyncInPlayMode(battleScene, new LoadSceneParameters(LoadSceneMode.Additive)).ToUniTask();
                await RunAsync("tasks/battle-entry", BattleGameEntry);
                await LiteRuntime.Instance.Shutdown();
                UnityEngine.Object.Destroy(LiteRuntime.Instance.Launcher.gameObject);
                await SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(battleScene)).ToUniTask();

                var launcher = new GameObject("TaskSystemValidationLauncher").AddComponent<LiteLauncher>();
                var deadline = Time.realtimeSinceStartup + 20f;
                while (LiteRuntime.Instance.State != LiteRuntime.RuntimeState.Running)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new Exception("Runtime startup timed out");
                    await UniTask.Yield();
                }
                // Keep the real coroutine host, but manually tick each isolated TaskSystem.
                launcher.enabled = false;

                await Run("tasks/admission", PriorityOrder);
                await RunAsync("tasks/cancel", CancelQueue);
                await Run("tasks/terminal", TerminalContract, "Validation execution failure");
                await RunAsync("tasks/coroutine-child", FlowParent);
                await RunAsync("tasks/download-result", WebGetSnapshots, "task-snapshot-missing.bin");
                await RunAsync("tasks/instantiate", InstantiationResults, "The Object you want to instantiate is null");
                launcher.enabled = true;
                try { await RunAsync("tasks/pool-ownership", _ => PoolValidation.Exercise(_current)); }
                finally { launcher.enabled = false; }
                await RunAsync("tasks/startup-rollback", async system =>
                {
                    await RuntimeInitializationReturnsFalse(system);
                    await RuntimeInitializationThrows(system);
                    await RuntimeShutdownDuringInitialization(system);
                }, $"Initialize {typeof(ShutdownSystemTwo).AssemblyQualifiedName} failed", "InjectedValidation runtime initialization", "Enter <ErrorState>", "FrameworkError: Startup");
                await RunAsync("tasks/ordered-shutdown", RuntimeShutdownOrder, "FrameworkError: Unknown");
                await RunAsync("tasks/settings-profiles", RuntimeSettingsProfiles, nameof(FailingCloseAppender));
                await RunAsync("tasks/late-tick", RuntimeLateTick);
                await RunAsync("tasks/restart", async system =>
                {
                    await RuntimeRestartStates(system);
                    await RuntimeRestartRequests(system);
                }, "Enter <ErrorState>", "FrameworkError: Startup", "FrameworkError: Unknown", "Cannot restart runtime in state Error.", $"Initialize {typeof(ShutdownSystemTwo).AssemblyQualifiedName} failed");
                await RunAsync("tasks/shutdown-failure", RuntimeShutdownFailure, "InjectedValidation partial system disposal");
            }
            catch (Exception ex)
            {
                Results.fatalError = ex.ToString();
            }
            finally
            {
                Application.logMessageReceived -= CaptureError;
                var editorStop = JsonUtility.FromJson<ValidationCase>(SessionState.GetString(EditorStopResultKey, string.Empty));
                Results.cases.Add(editorStop);
                Results.cases.Add(JsonUtility.FromJson<ValidationCase>(SessionState.GetString(LauncherSettingsResultKey, string.Empty)));
                Results.Save("tasks");
                EditorApplication.Exit(Results.fatalError == null && Results.unexpectedErrors.Count == 0 && Results.cases.All(c => c.passed) ? 0 : 2);
            }
        }

        private static async UniTask BattleGameEntry(TaskSystem system)
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

        private static UniTask Run(string name, Action<TaskSystem> test, params string[] expectedErrors)
        {
            return RunAsync(name, system => { test(system); return UniTask.CompletedTask; }, expectedErrors);
        }

        private static async UniTask RunAsync(string name, Func<TaskSystem, UniTask> test, params string[] expectedErrors)
        {
            _current = new ValidationCase { id = name };
            _expectedErrors = expectedErrors;
            Results.cases.Add(_current);
            var start = Time.realtimeSinceStartup;
            var originalLimit = LiteRuntime.Setting.Task.ConcurrencyLimit;
            LiteRuntime.Setting.Task.ConcurrencyLimit = 1;
            TaskSystem system = null;
            try
            {
                system = new TaskSystem();
                LiteRuntime.Setting.Task.ConcurrencyLimit = originalLimit;
                await system.Initialize();
                await test(system);
                _current.passed = true;
            }
            catch (Exception ex)
            {
                _current.failure = ex.ToString();
            }
            finally
            {
                LiteRuntime.Setting.Task.ConcurrencyLimit = originalLimit;
                try { if (system != null) await system.DisposeAsync(); }
                catch (Exception ex) { _current.passed = false; _current.failure += "\nCleanup: " + ex; }
                if (_current.errors.Count > 0)
                {
                    _current.passed = false;
                    _current.failure += "\nUnexpected errors: " + string.Join("\n", _current.errors);
                }
                _current.seconds = Time.realtimeSinceStartup - start;
                Debug.Log($"TASK_SYSTEM_CASE: {name}: {(_current.passed ? "PASS" : "FAIL")}");
                _current = null;
                _expectedErrors = Array.Empty<string>();
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

        private static List<ITask> Snapshot(TaskSystem system)
        {
            var tasks = new List<ITask>();
            system.CopyTasksTo(tasks);
            return tasks;
        }

        private static void PriorityOrder(TaskSystem system)
        {
            var blocker = new ProbeTask(); system.AddTask(blocker); system.Tick(0);
            var order = new List<string>();
            var expected = new[] { "urgent", "high-a", "high-b", "normal-a", "normal-b", "low" };
            var tasks = new Dictionary<string, ProbeTask>();
            void Add(string name, TaskPriority priority)
            {
                var task = new ProbeTask { ExecuteAction = _ => order.Add(name) };
                tasks.Add(name, task); system.AddTask(task, priority: priority);
            }
            Add("low", TaskPriority.Low); Add("normal-a", TaskPriority.Normal);
            Add("high-a", TaskPriority.High); Add("high-b", TaskPriority.High);
            Add("urgent", TaskPriority.Urgent); Add("normal-b", TaskPriority.Normal);
            system.Tick(0);
            Check(order.Count == 0 && !blocker.IsDone, "Higher priority never preempts a running task");
            Throws<InvalidOperationException>(() => system.AddTask(blocker), "Duplicate submission is rejected.");
            var independent = new ProbeTask();
            Throws<ArgumentException>(() => system.AddTask(independent, 17), "An unregistered group rejects submission.");
            system.RegisterGroup(17, 1);
            Throws<InvalidOperationException>(() => system.AddTask(tasks["low"], 17), "A queued task cannot also be submitted to another group.");
            system.AddTask(independent, 17);
            system.Tick(0);
            Check(independent.IsExecuted && order.Count == 0, "The previously rejected task runs in the newly registered group while Default is full.");
            independent.Finish();
            blocker.Finish();
            for (var i = 0; i < expected.Length; i++)
            {
                system.Tick(0);
                Check(order.Count == i + 1 && order[i] == expected[i], "Priority/FIFO position " + i + " is " + expected[i]);
                tasks[expected[i]].Finish();
            }
            system.Tick(0);
            Check(system.RunningTaskCount == 0 && system.PendingTaskCount == 0 && Snapshot(system).Count == 0, "Completed priority queue fully retires");
            NewGroupBoundary(system);
            BoundedChain(system);
        }

        private static void NewGroupBoundary(TaskSystem system)
        {
            var child = new ProbeTask();
            var parent = new ProbeTask { ExecuteAction = task =>
            {
                system.RegisterGroup(27, 1); system.AddTask(child, 27);
                Throws<ArgumentException>(() => system.RegisterGroup(27, 1), "Callback group registration is immediately visible");
                task.Finish();
            }};
            system.AddTask(parent); system.Tick(0);
            Check(child.Executions == 0 && system.PendingTaskCount == 1, "New group is outside the current group iteration bound");
            system.Tick(0);
            Check(child.Executions == 1, "New group participates next tick");
            child.Finish();
            system.Tick(0);
        }

        private static void BoundedChain(TaskSystem system)
        {
            var starts = 0;
            Action<ProbeTask> spawn = null;
            spawn = task => { starts++; system.AddTask(new ProbeTask { ExecuteAction = spawn }, LiteConst.TaskGroupId.Flow); task.Finish(); };
            system.AddTask(new ProbeTask { ExecuteAction = spawn }, LiteConst.TaskGroupId.Flow);
            for (var i = 1; i <= 4; i++)
            {
                system.Tick(0);
                Check(starts == i && system.PendingTaskCount == 1 && system.RunningTaskCount == 0, "Flow chain stops at this tick's admission bound: " + i);
            }
        }

        private static void TerminalContract(TaskSystem system)
        {
            var error = new InvalidOperationException("Expected task error");
            var failed = new ProbeTask();
            var early = failed.Task.GetAwaiter();
            failed.TerminalAction = task => Check(task.State == TaskState.Failed && ReferenceEquals(task.Error, error)
                && task.Result == null, "The termination hook sees the complete failure snapshot.");
            failed.Fail(error);
            failed.Cancel(); failed.Fail(new Exception("Late error")); failed.Finish(); failed.Dispose();
            Check(failed.Terminations == 1 && failed.State == TaskState.Failed && ReferenceEquals(failed.Error, error)
                && !failed.IsExecuted, "Pending failure is irreversible and survives Dispose.");
            Check(early.IsCompleted && early.GetResult() == null && failed.Task.GetAwaiter().GetResult() == null,
                "Failure keeps null-result await semantics before and after completion.");

            var executionError = new InvalidOperationException("Validation execution failure");
            var throwing = new ProbeTask { ExecuteAction = _ => throw executionError };
            system.AddTask(throwing);
            system.Tick(0);
            Check(throwing.State == TaskState.Failed && ReferenceEquals(throwing.Error, executionError) &&
                throwing.Disposals == 1, "Scheduler propagates execution failure and releases the occupied slot.");

            var late = new ProbeTask(); late.Fail(error);
            Check(late.Task.GetAwaiter().GetResult() == null && ReferenceEquals(late.Error, error),
                "A completion source first created after failure publishes the same result.");
            Throws<ArgumentNullException>(() => new ProbeTask().Fail(null), "Fail rejects a missing error.");

            var succeeded = new ProbeTask();
            Check(!succeeded.FinishResult(true) && succeeded.State == TaskState.Pending,
                "A task cannot complete successfully before execution.");
            system.AddTask(succeeded); system.Tick(0);
            Check(succeeded.FinishResult(null), "Null is a valid successful result for a generic task.");
            succeeded.Fail(error); succeeded.Cancel(); system.Tick(0);
            Check(succeeded.State == TaskState.Succeeded && succeeded.Error == null && succeeded.Progress == 1f
                && succeeded.Terminations == 1, "Success cannot be rewritten by late failure or cancellation.");
            var canceled = new ProbeTask(); canceled.Cancel(); canceled.Fail(error); canceled.Finish();
            Check(canceled.State == TaskState.Cancelled && canceled.Error == null && canceled.Terminations == 1
                && canceled.Task.GetAwaiter().GetResult() == null, "Cancellation is distinct from failure and remains final.");
        }

        private static async UniTask FlowParent(TaskSystem system)
        {
            var child = new ProbeTask(); var resumed = false;
            var parent = new CoroutineTask(ParentRoutine(system, child, () => resumed = true), null);
            system.AddTask(parent, LiteConst.TaskGroupId.Flow); system.Tick(0);
            Check(parent.IsExecuted && !child.IsExecuted && system.PendingTaskCount == 1, "Flow parent submits Default child after Default's current turn");
            system.Tick(0);
            await UniTask.Yield(); await UniTask.Yield();
            Check(child.IsExecuted && system.RunningTaskCount == 2 && !resumed && !parent.IsDone,
                "UniTask coroutine adapter waits across frames without holding the Default child's slot");
            child.Finish(); await WaitDone(system, parent);
            Check(resumed && child.Disposals == 1 && system.RunningTaskCount == 0, "Real coroutine parent resumes after child without quota deadlock");

            foreach (var state in new[] { TaskState.Failed, TaskState.Cancelled })
            {
                child = new ProbeTask(); resumed = false;
                parent = new CoroutineTask(ParentRoutine(system, child, () => resumed = true), null);
                system.AddTask(parent, LiteConst.TaskGroupId.Flow); system.Tick(0); system.Tick(0);
                await UniTask.Yield(); await UniTask.Yield();
                Check(!resumed && !parent.IsDone && child.State == TaskState.Running,
                    "Coroutine adapter waits before child " + state);
                var error = new InvalidOperationException("Expected adapted child failure");
                if (state == TaskState.Failed) child.Fail(error); else child.Cancel();
                await WaitDone(system, parent);
                Check(resumed && parent.State == TaskState.Succeeded && child.State == state
                    && child.Result == null && (state == TaskState.Failed ? ReferenceEquals(child.Error, error) : child.Error == null)
                    && child.Disposals == 1 && system.RunningTaskCount == 0,
                    "Coroutine adapter resumes without propagating child " + state + " or taking its cleanup ownership");
            }

            child = new ProbeTask(); resumed = false;
            parent = new CoroutineTask(ParentRoutine(system, child, () => resumed = true), null);
            system.AddTask(parent, LiteConst.TaskGroupId.Flow); system.Tick(0); system.Tick(0);
            parent.Cancel(); system.Tick(0); await UniTask.Yield();
            Check(!resumed && parent.State == TaskState.Cancelled && child.State == TaskState.Running
                && child.Disposals == 0 && system.RunningTaskCount == 1,
                "Cancelling the parent only releases its adapter and leaves the child scheduled");
            child.Finish(); system.Tick(0); await UniTask.Yield();
            Check(!resumed && child.State == TaskState.Succeeded && child.Disposals == 1 && system.RunningTaskCount == 0,
                "Child completion after parent cancellation cannot resume the disposed coroutine");
        }

        private static async UniTask InstantiationResults(TaskSystem system)
        {
            var failures = 0;
            var invalid = new InstantiateGameObjectTask(null, null, 1, result =>
            {
                failures++;
                Check(result.Length == 0, "Instantiation failure delivers an empty callback result.");
            });
            system.AddTask(invalid); await WaitDone(system, invalid);
            Check(invalid.State == TaskState.Failed && invalid.Error != null && failures == 1,
                "Native instantiation startup exceptions fail and notify once.");

            var template = new GameObject("TaskInstantiationValidation");
            var parent = new GameObject("TaskInstantiationResults");
            try
            {
                var callbacks = 0;
                var success = new InstantiateGameObjectTask(template, parent.transform, 2, _ => callbacks++);
                var earlyWait = success.Task.GetAwaiter();
                earlyWait.OnCompleted(() => success.Dispose());
                system.AddTask(success); await WaitDone(system, success);
                Check(success.State == TaskState.Succeeded && success.Error == null && callbacks == 1
                    && success.Result is GameObject[] { Length: 2 }, "Native success preserves result after scheduler disposal.");
                Check(((GameObject[])earlyWait.GetResult()).All(go => go != null),
                    "Retiring the wrapper from the success continuation preserves both instances and callback delivery.");
                var canceled = new InstantiateGameObjectTask(template, parent.transform, 2, _ => callbacks++);
                system.AddTask(canceled); system.Tick(0);
                canceled.Cancel(); canceled.Cancel(); system.Tick(0); await UniTask.Yield();
                Check(canceled.State == TaskState.Cancelled && canceled.Error == null && callbacks == 1,
                    "Native cancellation cannot reenter a success callback or rewrite the terminal state.");

                // Hold only the completion notification, so real native results exist before cancellation.
                var undelivered = new InstantiateGameObjectTask(template, null, 2, _ => callbacks++);
                system.AddTask(undelivered); system.Tick(0);
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var native = (AsyncInstantiateOperation<GameObject>)typeof(InstantiateGameObjectTask)
                    .GetField("_task", flags).GetValue(undelivered);
                var handler = (Action<AsyncOperation>)Delegate.CreateDelegate(typeof(Action<AsyncOperation>),
                    undelivered, typeof(InstantiateGameObjectTask).GetMethod("OnInstantiateComplete", flags));
                native.completed -= handler;
                native.WaitForCompletion();
                var objects = native.Result;
                try
                {
                    Check(objects.Length == 2 && objects.All(go => go != null) && !undelivered.IsDone,
                        "Native completion can precede publication of instantiated objects.");
                    undelivered.Cancel(); system.Tick(0);
                    await UniTask.NextFrame();
                    Check(objects.All(go => go == null) && callbacks == 1,
                        "Cancel destroys completed unpublished instances without relying on their parent or sending success.");
                    success.Cancel();
                    Check(((GameObject[])success.Result).All(go => go != null),
                        "Later cancellation never destroys an already published successful result.");
                }
                finally
                {
                    foreach (var go in objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
                UnityEngine.Object.DestroyImmediate(template);
            }
        }

        private static async UniTask WebGetSnapshots(TaskSystem system)
        {
            var scratch = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "Scratch");
            Directory.CreateDirectory(scratch);
            var binaryPath = Path.Combine(scratch, "task-snapshot-binary.bin");
            var emptyPath = Path.Combine(scratch, "task-snapshot-empty.bin");
            var expected = new byte[] { 0, 255, 128, 13, 10, 65, 0, 195, 40 };
            File.WriteAllBytes(binaryPath, expected);
            File.WriteAllBytes(emptyPath, Array.Empty<byte>());
            try
            {
                var blocker = new ProbeTask();
                system.AddTask(blocker); system.Tick(0);
                var calls = 0;
                byte[] delivered = null;
                var task = system.AddUnityWebGetRequestTask(new Uri(binaryPath).AbsoluteUri,
                    new RetryParam(1, 0, 0.01f), false, data => { calls++; delivered = data; });
                system.Tick(0);
                Check(task.State == TaskState.Pending && !task.IsExecuted && calls == 0,
                    "The WebGet factory still waits for Default capacity before creating a request.");
                Check(system.RunningTaskCount == 1 && system.PendingTaskCount == 1,
                    "A queued snapshot request cannot bypass the existing quota.");
                blocker.Finish();
                await WaitDone(system, task);
                Check(task.State == TaskState.Succeeded && task.Error == null
                    && calls == 1 && delivered.SequenceEqual(expected),
                    "The factory delivers the requested binary data exactly once.");

                await UniTask.Yield();
                var late = (byte[])await task.Task;
                Check(ReferenceEquals(late, delivered) && ReferenceEquals(late, task.Result) && late.SequenceEqual(expected),
                    "The first await after cleanup and another frame returns the same readable snapshot.");

                var empty = system.AddUnityWebGetRequestTask(new Uri(emptyPath).AbsoluteUri,
                    new RetryParam(1, 0, 0.01f), false, callback: null);
                await WaitDone(system, empty);
                Check(empty.State == TaskState.Succeeded && empty.Error == null && empty.Result is byte[] { Length: 0 },
                    "An empty response succeeds with an empty array.");
                var absent = system.AddUnityWebGetRequestTask(new Uri(Path.Combine(scratch, "task-snapshot-missing.bin")).AbsoluteUri,
                    new RetryParam(1, 0, 0.01f), false, callback: null);
                await WaitDone(system, absent);
                Check(absent.State == TaskState.Failed && absent.Error != null && await absent.Task == null,
                    "A missing file reports failure rather than an empty successful snapshot.");
                var lateEmpty = (byte[])await empty.Task;
                Check(lateEmpty.Length == 0 && ReferenceEquals(lateEmpty, empty.Result)
                    && Snapshot(system).Count == 0 && system.RunningTaskCount == 0 && system.PendingTaskCount == 0,
                    "An unobserved empty success remains available to a late await without retaining a scheduler slot.");
            }
            finally
            {
                File.Delete(binaryPath);
                File.Delete(emptyPath);
            }
        }

        private static async UniTask WaitDone(TaskSystem system, ITask task)
        {
            var deadline = Time.realtimeSinceStartup + 10f;
            while (!task.IsDone)
            {
                if (Time.realtimeSinceStartup > deadline) throw new Exception("Task completion timed out");
                await UniTask.Yield();
                system.Tick(0.016f);
            }
            system.Tick(0.016f);
        }

        private static IEnumerator ParentRoutine(TaskSystem system, ProbeTask child, Action resumed)
        {
            system.AddTask(child);
            ITask view = child;
            yield return view.Task.ToCoroutine();
            resumed();
        }

        private static void CaptureError(string message, string stack, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
            {
                if (_current != null && _expectedErrors.Any(message.Contains))
                {
                    _current.evidence.Add("Expected task error log: " + message);
                    return;
                }
                if (_current != null) _current.errors.Add(message);
                Results.unexpectedErrors.Add(message + "\n" + stack);
            }
        }

        private sealed class ProbeTask : BaseTask
        {
            public int Executions;
            public int Ticks;
            public int Disposals;
            public int Terminations;
            public Action<ProbeTask> ExecuteAction;
            public Action<ProbeTask> TickAction;
            public Action DisposeAction;
            public Action<ProbeTask> TerminalAction;
            public void Finish() => Complete(true);
            public bool FinishResult(object result) => Complete(result);
            protected override void OnTerminated() { Terminations++; TerminalAction?.Invoke(this); }
            protected override void OnExecute() { Executions++; ExecuteAction?.Invoke(this); }
            protected override void OnTick(float deltaTime) { Ticks++; TickAction?.Invoke(this); }
            protected override void OnDispose() { Disposals++; DisposeAction?.Invoke(); }
        }

        private static async UniTask CancelQueue(TaskSystem system)
        {
            var running = new ProbeTask();
            var waiting = new ProbeTask();
            var successor = new ProbeTask();
            system.AddTask(running);
            system.AddTask(waiting);
            system.AddTask(successor);
            system.Tick(0);
            waiting.Cancel();
            running.Cancel();
            system.Tick(0);
            Check(waiting.Executions == 0 && waiting.Disposals == 1 && running.Disposals == 1 && successor.IsExecuted,
                "Queued cancellation starts no work and running cancellation releases exactly one slot.");
            var completion = successor.Task;
            await system.DisposeAsync();
            Check(completion.Status == UniTaskStatus.Succeeded && await completion == null &&
                successor.State == TaskState.Cancelled && successor.Disposals == 1, "Shutdown settles the accepted task and releases it once.");
        }
    }
}
