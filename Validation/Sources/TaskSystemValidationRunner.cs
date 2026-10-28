using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace LiteQuark.Editor
{
    // Local-only harness against the unmodified production source snapshot.
    internal static class TaskSystemValidationRunner
    {
        private const string PendingKey = "LiteQuark.TaskSystemValidation.Pending";
        private const string ReportKey = "LiteQuark.TaskSystemValidation.Report";
        private static readonly Report Results = new();
        private static bool _running;
        private static CaseResult _current;
        private static string[] _expectedErrors = Array.Empty<string>();

        [Serializable]
        private sealed class Report
        {
            public string unityVersion;
            public string finishedUtc;
            public string fatalError;
            public long steadyTickAllocatedBytes = -1;
            public List<string> unexpectedErrors = new();
            public List<CaseResult> cases = new();
        }

        [Serializable]
        private sealed class CaseResult
        {
            public string name;
            public bool passed;
            public int assertions;
            public string failure;
            public List<string> evidence = new();
        }

        public static void Begin()
        {
            SessionState.SetString(ReportKey, Path.Combine(ValidationEnvironment.Reports("tasks"), "results.json"));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += StartIfPending;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) StartIfPending();
        }

        private static void StartIfPending()
        {
            if (_running || !EditorApplication.isPlaying || !SessionState.GetBool(PendingKey, false)) return;
            _running = true;
            SessionState.SetBool(PendingKey, false);
            RunAll().Forget();
        }

        private static async UniTask RunAll()
        {
            Results.unityVersion = Application.unityVersion;
            Application.logMessageReceived += CaptureError;
            try
            {
                var launcher = new GameObject("TaskSystemValidationLauncher").AddComponent<LiteLauncher>();
                var deadline = Time.realtimeSinceStartup + 20f;
                while (LiteRuntime.Instance.State != LiteRuntime.RuntimeState.Running)
                {
                    if (Time.realtimeSinceStartup > deadline) throw new Exception("Runtime startup timed out");
                    await UniTask.Yield();
                }
                // Keep the real coroutine host, but manually tick each isolated TaskSystem.
                launcher.enabled = false;

                await Run("01 Registration and one-shot submission", Registration);
                await Run("02 Independent group quotas and no priority bypass", IndependentGroups);
                await Run("03 Priority ordering, FIFO ties and non-preemption", PriorityOrder);
                await Run("04 Synchronous completion reuses quota", SynchronousCompletion);
                await Run("05 Queued cancellation never executes", QueuedCancellation);
                await Run("06 Running cancellation releases quota once", RunningCancellation);
                await Run("07 Tick callback submission and duplicate rejection", CallbackSubmission);
                await Run("08 Bounded synchronous Flow task chain", BoundedChain);
                await Run("09 Dispose callback submission survives compaction", DisposeSubmission);
                await Run("10 Newly registered groups start next tick", NewGroupBoundary);
                await Run("11 Convenience routing with saturated Default", ConvenienceRouting);
                await RunAsync("12 AsyncResult observer bypasses Default quota", ObserveAsyncResult);
                await RunAsync("13 Real AsyncOperation observer completes in Flow", ObserveAsyncOperation);
                await RunAsync("14 Coroutine Flow parent waits for Default child", FlowParent);
                await Run("15 Steady Tick and statistics allocations", SteadyAllocations);
                await RunAsync("16 Final disposal clears pending and running tasks", FinalDisposal);
                await Run("17 SafeList nested, early exit and exception cleanup", SafeListIteration);
                await Run("18 Fault retirement preserves quotas and disposal", FaultRetirement, "InjectedValidation", "FaultTask");
                await Run("19 Cancellation during admission skips canceled peer", AdmissionCancellation);
                await Run("20 Terminal state and early/late await contract", TerminalContract);
                await Run("21 Callback and main-thread execution failures", CallbackFailures, "InjectedValidation", "FaultTask");
                await RunAsync("22 Coroutine faults and iterator cleanup", CoroutineFailures);
                await RunAsync("23 Download creation and native callback failures", DownloadFailures, "task-terminal-missing.txt");
                await Run("24 Version failure and null-error fallback", VersionFailures,
                    "VersionPack is invalid", "VersionPack parse failed", "InjectedValidation version parse");
                await RunAsync("25 Native instantiation success, failure and cancellation", InstantiationResults,
                    "The Object you want to instantiate is null");
                await Run("26 Lifecycle access and controlled submission", LifecycleAccess);
                await Run("27 Cleanup is idempotent and preserves terminal state", CleanupIdempotency, "InjectedValidation", "FaultTask");
                await RunAsync("28 Result delivery preserves callback and await order", ResultDelivery,
                    "task-result-missing.txt", "InjectedValidation result callback");
                await RunAsync("29 WebGet byte snapshots outlive request cleanup", WebGetSnapshots);
                await RunAsync("30 Runtime shutdown awaits logic and systems in order", RuntimeShutdownOrder,
                    "FrameworkError: Unknown, Framework error");
                await RunAsync("31 Runtime cleans systems after initialization returns false", RuntimeInitializationReturnsFalse,
                    $"Initialize {typeof(ShutdownSystemTwo).AssemblyQualifiedName} failed", "Enter <ErrorState>", "FrameworkError: Startup");
                await RunAsync("32 Runtime cleans unpublished logic after initialization throws", RuntimeInitializationThrows,
                    "InjectedValidation runtime initialization", "Enter <ErrorState>", "FrameworkError: Startup");
                await RunAsync("33 Runtime shutdown waits for in-flight initialization", RuntimeShutdownDuringInitialization);
            }
            catch (Exception ex)
            {
                Results.fatalError = ex.ToString();
            }
            finally
            {
                Application.logMessageReceived -= CaptureError;
                Results.finishedUtc = DateTime.UtcNow.ToString("O");
                var path = SessionState.GetString(ReportKey, string.Empty);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(Results, true));
                var success = Results.fatalError == null && Results.unexpectedErrors.Count == 0
                    && Results.cases.Count == 33 && Results.cases.All(c => c.passed);
                Debug.Log($"TASK_SYSTEM_QUICK: {Results.cases.Count(c => c.passed)}/{Results.cases.Count}, assertions={Results.cases.Sum(c => c.assertions)}, GC={Results.steadyTickAllocatedBytes}");
                EditorApplication.Exit(success ? 0 : 2);
            }
        }

        private static UniTask Run(string name, Action<TaskSystem> test, params string[] expectedErrors)
        {
            return RunAsync(name, system => { test(system); return UniTask.CompletedTask; }, expectedErrors);
        }

        private static async UniTask RunAsync(string name, Func<TaskSystem, UniTask> test, params string[] expectedErrors)
        {
            _current = new CaseResult { name = name };
            _expectedErrors = expectedErrors;
            Results.cases.Add(_current);
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
                Debug.Log($"TASK_SYSTEM_CASE: {name}: {(_current.passed ? "PASS" : "FAIL")}");
                _current = null;
                _expectedErrors = Array.Empty<string>();
            }
        }

        private static void Check(bool condition, string evidence)
        {
            _current.assertions++;
            if (!condition) throw new Exception(evidence);
            _current.evidence.Add(evidence);
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

        private static void Registration(TaskSystem system)
        {
            Throws<ArgumentException>(() => system.RegisterGroup(0, 1), "Default ID is reserved");
            Throws<ArgumentException>(() => system.RegisterGroup(LiteConst.TaskGroupId.Flow, null), "Flow ID is reserved");
            Throws<ArgumentOutOfRangeException>(() => system.RegisterGroup(9, 0), "Zero quota rejected");
            Throws<ArgumentNullException>(() => system.AddTask((ITask)null), "Null task rejected");
            Throws<ArgumentException>(() => system.AddTask(new ProbeTask(), 99), "Unknown group rejected before registration");
            system.RegisterGroup(1, 2);
            var task = new ProbeTask();
            system.AddTask(task);
            Throws<InvalidOperationException>(() => system.AddTask(task, 1), "Same object cannot enter two groups");
            Check(system.PendingTaskCount == 1 && Snapshot(system).Count == 1, "Rejected submissions leave no orphan records");
            task.Cancel(); system.Tick(0);
            Throws<InvalidOperationException>(() => system.AddTask(task), "Terminal task cannot be reused");
            Check(task.Executions == 0 && task.Disposals == 1, "Pre-start cancellation is disposed once");
        }

        private static void IndependentGroups(TaskSystem system)
        {
            system.RegisterGroup(1, 1); system.RegisterGroup(2, 2);
            var a = new ProbeTask(); var a2 = new ProbeTask();
            var b = new ProbeTask(); var b2 = new ProbeTask(); var b3 = new ProbeTask();
            system.AddTask(a, 1); system.AddTask(b, 2); system.AddTask(b2, 2);
            system.Tick(0);
            system.AddTask(a2, 1, TaskPriority.Urgent); system.AddTask(b3, 2);
            system.Tick(0);
            Check(a.Executions == 1 && b.Executions == 1 && b2.Executions == 1, "Groups admit one and two tasks independently");
            Check(system.RunningTaskCount == 3 && system.PendingTaskCount == 2, "Total running exceeds Default quota without a global cap");
            Check(a2.Executions == 0 && b3.Executions == 0, "Urgent does not bypass a full group");
            a.Finish(); system.Tick(0);
            Check(a2.Executions == 1 && b3.Executions == 0, "Freeing group A does not alter group B quota");
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
            blocker.Finish();
            for (var i = 0; i < expected.Length; i++)
            {
                system.Tick(0);
                Check(order.Count == i + 1 && order[i] == expected[i], "Priority/FIFO position " + i + " is " + expected[i]);
                tasks[expected[i]].Finish();
            }
            system.Tick(0);
            Check(system.RunningTaskCount == 0 && system.PendingTaskCount == 0 && Snapshot(system).Count == 0, "Completed priority queue fully retires");
        }

        private static void SynchronousCompletion(TaskSystem system)
        {
            var tasks = Enumerable.Range(0, 12).Select(_ => new ProbeTask { ExecuteAction = task => task.Finish() }).ToArray();
            foreach (var task in tasks) system.AddTask(task);
            system.Tick(0);
            Check(tasks.All(t => t.Executions == 1 && t.Disposals == 1), "Twelve synchronous tasks share one quota in one tick");
            Check(tasks.All(t => t.Ticks == 0), "Newly started tasks receive no same-tick Tick call");
            Check(system.RunningTaskCount == 0 && system.PendingTaskCount == 0 && Snapshot(system).Count == 0, "Synchronous completion clears all accounting");
        }

        private static void QueuedCancellation(TaskSystem system)
        {
            var active = new ProbeTask(); var canceled = new ProbeTask(); var next = new ProbeTask();
            system.AddTask(active); system.Tick(0);
            system.AddTask(canceled, priority: TaskPriority.Urgent); system.AddTask(next);
            var awaiter = canceled.Task.GetAwaiter();
            canceled.Cancel(); system.Tick(0);
            Check(canceled.Executions == 0 && canceled.Disposals == 1, "Canceled queued task never starts");
            Check(awaiter.IsCompleted && awaiter.GetResult() == null, "Cancellation preserves null-result await semantics");
            Check(system.RunningTaskCount == 1 && system.PendingTaskCount == 1, "Queued cancellation does not consume or release another task's slot");
            active.Finish(); system.Tick(0);
            Check(next.Executions == 1 && !Snapshot(system).Contains(canceled), "Next queued task starts and canceled record is absent");
        }

        private static void RunningCancellation(TaskSystem system)
        {
            var first = new ProbeTask(); var second = new ProbeTask();
            system.AddTask(first); system.AddTask(second); system.Tick(0);
            first.Cancel(); system.Tick(0); system.Tick(0);
            Check(first.Disposals == 1 && second.Executions == 1, "Running cancellation retires once and admits successor");
            Check(system.RunningTaskCount == 1 && system.PendingTaskCount == 0, "Running cancellation keeps exact counts");
        }

        private static void CallbackSubmission(TaskSystem system)
        {
            var child = new ProbeTask(); var parent = new ProbeTask();
            parent.TickAction = task =>
            {
                system.AddTask(child);
                Throws<InvalidOperationException>(() => system.AddTask(child), "Reentrant duplicate submission is rejected immediately");
                task.Finish();
            };
            system.AddTask(parent); system.Tick(0); system.Tick(0);
            Check(child.Executions == 1 && child.Ticks == 0, "Callback-added task can start but cannot Tick in the same pass");
            system.Tick(0);
            Check(child.Ticks == 1 && parent.Disposals == 1, "Appended child is retained and advances next tick");
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

        private static void DisposeSubmission(TaskSystem system)
        {
            var replacements = new List<ProbeTask>();
            var originals = new List<ProbeTask>();
            for (var i = 0; i < 3; i++)
            {
                var task = new ProbeTask { ExecuteAction = t => t.Finish(), DisposeAction = () =>
                {
                    var replacement = new ProbeTask(); replacements.Add(replacement); system.AddTask(replacement);
                }};
                originals.Add(task); system.AddTask(task);
            }
            system.Tick(0);
            var registered = Snapshot(system);
            Check(originals.All(t => t.Disposals == 1), "All original records are retired once");
            Check(registered.Count == 3 && registered.SequenceEqual(replacements.Cast<ITask>()), "Compaction preserves callback-appended records and their order");
            Check(system.PendingTaskCount == 3 && system.RunningTaskCount == 0, "Admission attempts do not expand with dispose callbacks");
            system.Tick(0);
            Check(replacements[0].Executions == 1, "Preserved replacement starts next tick");
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
        }

        private static void ConvenienceRouting(TaskSystem system)
        {
            var blocker = new ProbeTask(); system.AddTask(blocker); system.Tick(0);
            var called = 0;
            var dispatch = system.AddMainThreadTask(_ => called++, null);
            var callback = system.AddTask(done => done(true));
            var coroutine = system.AddTask(EmptyRoutine());
            var direct = new MainThreadTask(_ => called += 100, null);
            system.AddTask((ITask)direct); system.Tick(0);
            Check(dispatch.IsDone && called == 1, "Main-thread convenience entry runs in Flow despite saturated Default");
            Check(!callback.IsExecuted && !coroutine.IsExecuted, "Opaque callback/coroutine conveniences remain in Default");
            Check(!direct.IsExecuted && system.PendingTaskCount == 3, "Generic ITask entry does not infer group from task type");
        }

        private static async UniTask ObserveAsyncResult(TaskSystem system)
        {
            system.AddTask(new ProbeTask()); system.Tick(0);
            var result = new ControlledResult(); var callbacks = 0;
            var observer = system.AddTask((IAsyncResult)result, () => callbacks++);
            system.Tick(0);
            Check(observer.IsExecuted && system.RunningTaskCount == 2, "IAsyncResult convenience entry does not consume Default quota");
            result.IsCompleted = true;
            await WaitDone(system, observer);
            Check(callbacks == 1 && system.RunningTaskCount == 1, "AsyncResult observer completes, calls back once and retires");
        }

        private static async UniTask ObserveAsyncOperation(TaskSystem system)
        {
            system.AddTask(new ProbeTask()); system.Tick(0);
            var operation = Resources.UnloadUnusedAssets(); var callbacks = 0;
            var observer = system.AddTask(operation, () => callbacks++);
            system.Tick(0);
            Check(observer.IsExecuted, "Real AsyncOperation observer starts with Default full");
            await WaitDone(system, observer);
            Check(operation.isDone && callbacks == 1 && system.RunningTaskCount == 1, "Real Unity operation observation completes and releases Flow record");
        }

        private static async UniTask FlowParent(TaskSystem system)
        {
            var child = new ProbeTask(); var resumed = false;
            var parent = new CoroutineTask(ParentRoutine(system, child, () => resumed = true), null);
            system.AddTask(parent, LiteConst.TaskGroupId.Flow); system.Tick(0);
            Check(parent.IsExecuted && !child.IsExecuted && system.PendingTaskCount == 1, "Flow parent submits Default child after Default's current turn");
            system.Tick(0);
            Check(child.IsExecuted && system.RunningTaskCount == 2, "Flow parent does not hold the Default child's slot");
            child.Finish(); await WaitDone(system, parent);
            Check(resumed && child.Disposals == 1 && system.RunningTaskCount == 0, "Real coroutine parent resumes after child without quota deadlock");
        }

        private static void SteadyAllocations(TaskSystem system)
        {
            var method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", Type.EmptyTypes);
            Check(method != null, "Unity runtime supports per-thread allocation accounting");
            var allocated = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
            system.AddTask(new ProbeTask()); system.Tick(0);
            var buffer = new List<ITask>(4); var sum = 0;
            for (var i = 0; i < 100; i++) { system.Tick(0); system.CopyTasksTo(buffer); sum += system.RunningTaskCount + system.PendingTaskCount; }
            allocated();
            var before = allocated();
            for (var i = 0; i < 1000; i++) { system.Tick(0); system.CopyTasksTo(buffer); sum += system.RunningTaskCount + system.PendingTaskCount; }
            Results.steadyTickAllocatedBytes = allocated() - before;
            Check(sum == 1100, "Allocation loop performed 1000 measured scheduler ticks after warmup");
            Check(Results.steadyTickAllocatedBytes == 0, "Steady Tick/statistics/reused debug copy allocated zero bytes");
            buffer.Clear();
        }

        private static void SafeListIteration(TaskSystem system)
        {
            var list = new SafeList<int>();
            list.Add(1); list.Add(2); list.Add(3);
            var visited = new List<int>();
            foreach (var item in list)
            {
                visited.Add(item);
                if (item == 1)
                {
                    list.Remove(2); list.Add(4);
                    foreach (var nested in list)
                    {
                        Check(nested == 1, "Nested enumeration sees the stable current snapshot.");
                        break;
                    }
                    Check(list.Count == 3 && list.Contains(2) && !list.Contains(4), "Nested disposal does not flush outer edits.");
                }
            }
            Check(visited.SequenceEqual(new[] { 1, 2, 3 }) && list.SequenceEqual(new[] { 1, 3, 4 }),
                "Outermost disposal applies deferred edits exactly once.");
            int ReturnEarly()
            {
                foreach (var item in list) { list.Remove(item); return item; }
                return -1;
            }
            Check(ReturnEarly() == 1 && list.SequenceEqual(new[] { 3, 4 }), "Early return flushes deferred removal.");
            try
            {
                foreach (var item in list) { list.Add(5); throw new InvalidOperationException("Expected enumeration exit"); }
            }
            catch (InvalidOperationException) { }
            list.Add(6);
            Check(list.SequenceEqual(new[] { 3, 4, 5, 6 }), "Exception exit releases iteration guard and allows immediate later writes.");
            foreach (var item in list) { list.Clear(); list.Add(7); break; }
            Check(list.Count == 1 && list[0] == 7, "Clear and later add preserve deferred operation order on break.");
        }

        private static void FaultRetirement(TaskSystem system)
        {
            var executeFault = new FaultTask { ThrowExecute = true, ThrowTermination = true, ThrowDispose = true };
            var tickFault = new FaultTask { ThrowTick = true, ThrowTermination = true, ThrowDispose = true };
            var successor = new ProbeTask();
            system.AddTask(executeFault); system.AddTask(tickFault); system.AddTask(successor);
            system.Tick(0);
            Check(executeFault.Terminations == 1 && executeFault.Disposals == 1 && executeFault.State == TaskState.Failed
                && ReferenceEquals(executeFault.Error, executeFault.ExecuteError),
                "Execute failure preserves the original exception and retires once despite termination/disposal faults.");
            Check(tickFault.IsExecuted && !successor.IsExecuted && system.RunningTaskCount == 1 && system.PendingTaskCount == 1,
                "Execute failure returns its quota for the next task.");
            system.Tick(0);
            Check(tickFault.Terminations == 1 && tickFault.Disposals == 1 && tickFault.State == TaskState.Failed
                && ReferenceEquals(tickFault.Error, tickFault.TickError) && successor.IsExecuted,
                "Tick failure retires once and the successor starts.");
            successor.Finish(); system.Tick(0); system.Tick(0);
            Check(Snapshot(system).Count == 0 && system.RunningTaskCount == 0 && system.PendingTaskCount == 0 &&
                executeFault.Disposals == 1 && tickFault.Disposals == 1, "Fault paths leave no quota leaks or repeated disposal.");
        }

        private static void AdmissionCancellation(TaskSystem system)
        {
            var canceled = new ProbeTask();
            var tail = new ProbeTask { ExecuteAction = task => task.Finish() };
            var first = new ProbeTask { ExecuteAction = task => { canceled.Cancel(); task.Finish(); } };
            system.AddTask(first); system.AddTask(canceled); system.AddTask(tail);
            system.Tick(0);
            Check(canceled.Executions == 0 && canceled.Disposals == 1, "A peer canceled after the sweep is retired without Execute.");
            Check(first.Disposals == 1 && tail.Executions == 1 && tail.Disposals == 1 &&
                system.PendingTaskCount == 0 && system.RunningTaskCount == 0, "Skipped cancellation consumes no quota and subsequent work completes.");
        }

        private sealed class FaultTask : BaseTask
        {
            public bool ThrowExecute;
            public bool ThrowTick;
            public bool ThrowTermination;
            public bool ThrowDispose;
            public int Terminations;
            public int Disposals;
            public readonly Exception ExecuteError = new InvalidOperationException("InjectedValidation Execute");
            public readonly Exception TickError = new InvalidOperationException("InjectedValidation Tick");
            protected override void OnExecute()
            {
                if (ThrowExecute) throw ExecuteError;
            }
            protected override void OnTick(float dt)
            {
                if (ThrowTick) throw TickError;
            }
            protected override void OnTerminated()
            {
                Terminations++;
                if (ThrowTermination) throw new InvalidOperationException("InjectedValidation Termination");
            }
            protected override void OnDispose()
            {
                Disposals++;
                if (ThrowDispose) throw new InvalidOperationException("InjectedValidation Dispose");
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

        private static void CallbackFailures(TaskSystem system)
        {
            Action<bool> done = null;
            var callback = new WaitCallbackTask(cb => done = cb);
            system.AddTask(callback); system.Tick(0); done(false);
            var error = callback.Error; done(true); done(false); system.Tick(0);
            Check(callback.State == TaskState.Failed && error != null && ReferenceEquals(callback.Error, error),
                "A false callback fails once; late successful callbacks cannot replace it.");

            var injected = new InvalidOperationException("InjectedValidation callback execution");
            var throwing = new WaitCallbackTask(_ => throw injected);
            var dispatch = new MainThreadTask(_ => throw injected, null);
            system.AddTask(throwing); system.AddTask(dispatch, LiteConst.TaskGroupId.Flow); system.Tick(0);
            Check(throwing.State == TaskState.Failed && dispatch.State == TaskState.Failed
                && ReferenceEquals(throwing.Error, injected) && ReferenceEquals(dispatch.Error, injected),
                "Callback and main-thread execution exceptions reach Task.Error.");

            var completed = new WaitCallbackTask(cb => { cb(true); throw injected; });
            system.AddTask(completed); system.Tick(0);
            Check(completed.State == TaskState.Succeeded && completed.Error == null
                && (bool)completed.Result && Snapshot(system).Count == 0,
                "An exception after published success is diagnostic and still returns the quota.");
        }

        private static async UniTask CoroutineFailures(TaskSystem system)
        {
            var error = new InvalidOperationException("Expected coroutine failure");
            var disposed = 0; var callbacks = 0;
            IEnumerator Child()
            {
                try { yield return null; throw error; }
                finally { disposed++; }
            }
            IEnumerator Parent()
            {
                try { yield return Child(); }
                finally { disposed++; }
            }
            var failed = new CoroutineTask(Parent(), () => callbacks++);
            system.AddTask(failed); await WaitDone(system, failed);
            Check(failed.State == TaskState.Failed && ReferenceEquals(failed.Error, error)
                && disposed == 2 && callbacks == 0, "Nested coroutine failure preserves the exception and disposes both iterators.");

            var current = new ThrowingCurrent(error);
            var currentTask = new CoroutineTask(current, () => callbacks++);
            system.AddTask(currentTask); await WaitDone(system, currentTask);
            Check(currentTask.State == TaskState.Failed && ReferenceEquals(currentTask.Error, error)
                && current.Disposals == 1, "IEnumerator.Current failure terminates and disposes once.");

            IEnumerator Waiting()
            {
                try { while (true) yield return null; }
                finally { disposed++; }
            }
            var canceled = new CoroutineTask(Waiting(), () => callbacks++);
            system.AddTask(canceled); system.Tick(0);
            canceled.Cancel(); canceled.Cancel(); system.Tick(0); await UniTask.Yield();
            Check(canceled.State == TaskState.Cancelled && canceled.Error == null && disposed == 3
                && callbacks == 0, "Cancel disposes a suspended iterator once without a success callback.");

            AsyncResultTask observer = null;
            var result = new ControlledResult { IsCompleted = true, OnRead = () => observer.Cancel() };
            observer = new AsyncResultTask(result, () => callbacks++);
            system.AddTask(observer); await WaitDone(system, observer);
            Check(observer.State == TaskState.Cancelled && callbacks == 0,
                "Cancellation during observer advancement cannot issue a success callback.");
        }

        private static async UniTask DownloadFailures(TaskSystem system)
        {
            var scratch = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "Scratch");
            Directory.CreateDirectory(scratch);
            var path = Path.Combine(scratch, "task-terminal-data.txt");
            File.WriteAllText(path, "validation");
            try
            {
                var uri = new Uri(path).AbsoluteUri;
                var error = new InvalidOperationException("Expected download failure");
                var creation = new DownloadProbe(uri) { CreateError = error };
                system.AddTask(creation); await WaitDone(system, creation);
                Check(creation.State == TaskState.Failed && ReferenceEquals(creation.Error, error)
                    && creation.Failures == 1 && creation.FailureWasPublished,
                    "Request creation errors notify failure once with a published Task.Error.");

                var completion = new DownloadProbe(uri) { CompletionError = error };
                system.AddTask(completion); await WaitDone(system, completion);
                completion.Cancel(); completion.Fail(new Exception("Late download error"));
                Check(completion.State == TaskState.Failed && ReferenceEquals(completion.Error, error)
                    && completion.Failures == 1, "Native request callback exceptions preserve the error and cannot become cancellation.");

                var missing = new DownloadProbe(new Uri(Path.Combine(scratch, "task-terminal-missing.txt")).AbsoluteUri);
                system.AddTask(missing); await WaitDone(system, missing);
                Check(missing.State == TaskState.Failed && missing.Error.Message.Contains("HTTP ")
                    && missing.Error.Message.Contains("task-terminal-missing.txt") && missing.Failures == 1,
                    "Final transport failure preserves URI and response status before request disposal.");
            }
            finally { File.Delete(path); }
        }

        private static void VersionFailures(TaskSystem system)
        {
            var fail = typeof(LoadVersionPackTask).GetMethod("FailVersionPack",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var calls = 0;
            var task = new LoadVersionPackTask("file:///validation/version.bytes", _ => calls++);
            fail.Invoke(task, new object[] { "VersionPack is invalid", null });
            task.Cancel(); task.Dispose();
            Check(task.State == TaskState.Failed && task.Error is InvalidOperationException
                && task.Error.Message == "VersionPack is invalid" && calls == 1,
                "Invalid version metadata creates a real error and delivers one failure callback.");
            var error = new FormatException("InjectedValidation version parse");
            var parsed = new LoadVersionPackTask("file:///validation/version.bytes", _ => calls++);
            fail.Invoke(parsed, new object[] { "VersionPack parse failed", error });
            parsed.Dispose();
            Check(parsed.State == TaskState.Failed && ReferenceEquals(parsed.Error, error) && calls == 2,
                "Version parse failure preserves the original exception and invokes its callback once.");
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
                system.AddTask(success); await WaitDone(system, success);
                Check(success.State == TaskState.Succeeded && success.Error == null && callbacks == 1
                    && success.Result is GameObject[] { Length: 2 }, "Native success preserves result after scheduler disposal.");
                var canceled = new InstantiateGameObjectTask(template, parent.transform, 2, _ => callbacks++);
                system.AddTask(canceled); system.Tick(0);
                canceled.Cancel(); canceled.Cancel(); system.Tick(0); await UniTask.Yield();
                Check(canceled.State == TaskState.Cancelled && canceled.Error == null && callbacks == 1,
                    "Native cancellation cannot reenter a success callback or rewrite the terminal state.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
                UnityEngine.Object.DestroyImmediate(template);
            }
        }

        private static void LifecycleAccess(TaskSystem system)
        {
            var taskType = typeof(BaseTask);
            var contract = typeof(ITask);
            Check(!typeof(ITick).IsAssignableFrom(contract) && !typeof(IDisposable).IsAssignableFrom(contract)
                && !typeof(ITick).IsAssignableFrom(taskType) && !typeof(IDisposable).IsAssignableFrom(taskType),
                "Task observation no longer exposes ITick or IDisposable control.");
            foreach (var name in new[] { "Execute", "Tick", "Dispose", "Fail" })
            {
                Check(contract.GetMethod(name) == null && taskType.GetMethod(name) == null,
                    name + " is absent from the public task contract.");
                var method = taskType.GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Check(method != null && !method.IsVirtual && (name == "Fail" ? method.IsFamilyOrAssembly : method.IsAssembly),
                    name + " is controlled by the framework, with failure also available to subclasses.");
            }
            var cleanup = taskType.GetMethod("OnDispose", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Check(cleanup != null && cleanup.IsFamily && cleanup.IsVirtual && !cleanup.IsAbstract,
                "Task subclasses customize cleanup through an optional protected hook.");
            Check(typeof(IEnumerator).IsAssignableFrom(contract) && contract.GetMethod("Cancel") != null
                && contract.GetProperty("Task") != null, "Cancellation and both existing waiting interfaces remain public.");

            var foreign = new ForeignTask();
            ITask unsupported = foreign;
            Throws<ArgumentException>(() => system.AddTask(unsupported), "Independent ITask implementations are explicitly rejected.");
            Check(foreign.Observations == 0 && system.PendingTaskCount == 0 && system.RunningTaskCount == 0
                && Snapshot(system).Count == 0, "Rejection neither observes or drives the object nor registers a coroutine wrapper.");

            var accepted = new ProbeTask();
            ITask view = accepted;
            system.AddTask(view);
            Check(!view.IsExecuted && Snapshot(system).Count == 1 && ReferenceEquals(Snapshot(system)[0], accepted),
                "An ITask-typed BaseTask keeps the original submission overload and deferred execution.");
            var awaiter = view.Task.GetAwaiter();
            system.Tick(0); view.Cancel(); system.Tick(0);
            Check(accepted.Executions == 1 && accepted.Disposals == 1 && view.State == TaskState.Cancelled
                && awaiter.IsCompleted && awaiter.GetResult() == null,
                "The public observation/cancellation interface retains its await semantics.");
        }

        private static void CleanupIdempotency(TaskSystem system)
        {
            var successful = new ProbeTask();
            successful.DisposeAction = () => successful.Dispose();
            var result = new object();
            system.AddTask(successful); system.Tick(0); successful.FinishResult(result); system.Tick(0);
            successful.Dispose();
            Check(successful.Disposals == 1 && successful.Terminations == 1 && successful.State == TaskState.Succeeded
                && successful.Error == null && ReferenceEquals(successful.Result, result)
                && ReferenceEquals(successful.Task.GetAwaiter().GetResult(), result),
                "Repeated and reentrant cleanup runs once without replacing the successful result.");

            var failed = new FaultTask { ThrowExecute = true, ThrowDispose = true };
            system.AddTask(failed); var waiter = failed.Task.GetAwaiter(); system.Tick(0);
            failed.Dispose(); failed.Dispose();
            Check(failed.Disposals == 1 && failed.Terminations == 1 && failed.State == TaskState.Failed
                && ReferenceEquals(failed.Error, failed.ExecuteError) && waiter.IsCompleted && waiter.GetResult() == null,
                "A throwing cleanup is attempted once and cannot overwrite the original failure or await result.");

            var canceled = new ProbeTask();
            system.AddTask(canceled); canceled.Cancel(); system.Tick(0); canceled.Dispose();
            Check(canceled.Executions == 0 && canceled.Disposals == 1 && canceled.Terminations == 1
                && canceled.State == TaskState.Cancelled && canceled.Error == null && Snapshot(system).Count == 0,
                "Queued cancellation still retires once with no execution or quota leak.");
        }

        private static async UniTask ResultDelivery(TaskSystem system)
        {
            const System.Reflection.BindingFlags privateInstance = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var scratch = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "Scratch");
            Directory.CreateDirectory(scratch);
            var textPath = Path.Combine(scratch, "task-result-data.txt");
            var packPath = Path.Combine(scratch, "task-result-version.bytes");
            File.WriteAllText(textPath, "result-delivery");
            File.WriteAllBytes(packPath, new VersionPackInfo("result-delivery", "windows", false, false, Array.Empty<BundleInfo>()).ToBinaryData());
            try
            {
                var events = new List<string>();
                var calls = 0;
                var matches = false;
                var callbackText = string.Empty;
                byte[] delivered = null;
                UnityWebGetRequestTask web = null;
                web = new UnityWebGetRequestTask(new Uri(textPath).AbsoluteUri, new RetryParam(1, 0, 0.01f), false, data =>
                {
                    calls++; events.Add("callback"); delivered = data;
                    callbackText = System.Text.Encoding.UTF8.GetString(data);
                    matches = ReferenceEquals(data, web.Result)
                        && typeof(UnityWebGetRequestTask).GetField("_callback", privateInstance).GetValue(web) == null;
                    if (calls == 1)
                    {
                        var request = typeof(UnityDownloadBaseTask).GetField("_request", privateInstance).GetValue(web);
                        typeof(UnityWebGetRequestTask).GetMethod("OnSuccess", privateInstance).Invoke(web, new[] { request });
                    }
                    throw new InvalidOperationException("InjectedValidation result callback");
                });
                var webWait = web.Task.GetAwaiter();
                webWait.OnCompleted(() => events.Add("await"));
                system.AddTask(web); await WaitDone(system, web);
                Check(web.State == TaskState.Succeeded && web.Error == null && calls == 1 && matches
                    && callbackText == "result-delivery" && events.SequenceEqual(new[] { "await", "callback" }),
                    "WebGet success publishes await first and consumes its callback before reentry or an observer exception.");
                await UniTask.Yield();
                Check(ReferenceEquals(webWait.GetResult(), delivered) && ReferenceEquals(web.Result, delivered)
                    && System.Text.Encoding.UTF8.GetString(delivered) == "result-delivery",
                    "WebGet keeps the same readable data snapshot after request cleanup and another frame.");

                events.Clear(); calls = 0; matches = false;
                LoadVersionPackTask version = null;
                VersionPackInfo deliveredPack = null;
                version = new LoadVersionPackTask(new Uri(packPath).AbsoluteUri, pack =>
                {
                    calls++; events.Add("callback"); deliveredPack = pack;
                    matches = ReferenceEquals(pack, version.Result)
                        && typeof(LoadVersionPackTask).GetField("_callback", privateInstance).GetValue(version) == null;
                    if (calls == 1)
                    {
                        var request = typeof(UnityDownloadBaseTask).GetField("_request", privateInstance).GetValue(version);
                        typeof(LoadVersionPackTask).GetMethod("OnSuccess", privateInstance).Invoke(version, new[] { request });
                    }
                });
                var versionWait = version.Task.GetAwaiter();
                versionWait.OnCompleted(() => events.Add("await"));
                system.AddTask(version); await WaitDone(system, version);
                Check(version.State == TaskState.Succeeded && version.Error == null && deliveredPack?.Version == "result-delivery"
                    && calls == 1 && matches && ReferenceEquals(versionWait.GetResult(), deliveredPack)
                    && events.SequenceEqual(new[] { "await", "callback" }),
                    "Repeated version success preserves the first result and sends one callback after await publication.");

                events.Clear(); calls = 0; matches = false;
                UnityWebGetRequestTask failed = null;
                failed = new UnityWebGetRequestTask(new Uri(Path.Combine(scratch, "task-result-missing.txt")).AbsoluteUri,
                    new RetryParam(1, 0, 0.01f), false, data =>
                    {
                        calls++; events.Add("callback");
                        matches = data == null && failed.State == TaskState.Failed && failed.Error != null
                            && typeof(UnityWebGetRequestTask).GetField("_callback", privateInstance).GetValue(failed) == null;
                        failed.Cancel();
                    });
                var failureWait = failed.Task.GetAwaiter();
                failureWait.OnCompleted(() => events.Add("await"));
                system.AddTask(failed); await WaitDone(system, failed);
                Check(calls == 1 && matches && failed.State == TaskState.Failed && failureWait.GetResult() == null
                    && events.SequenceEqual(new[] { "callback", "await" }),
                    "Failure callbacks retain their existing order before await, even with cancellation reentry.");

                var canceled = new UnityWebGetRequestTask(new Uri(textPath).AbsoluteUri, new RetryParam(1, 0, 0.01f), false, _ => calls++);
                system.AddTask(canceled); canceled.Cancel(); system.Tick(0);
                Check(canceled.State == TaskState.Cancelled && canceled.Task.GetAwaiter().GetResult() == null && calls == 1,
                    "WebGet cancellation still completes await without invoking a completion callback.");

                events.Clear(); calls = 0;
                var requestSurvivedDispose = false;
                var requestField = typeof(UnityDownloadBaseTask).GetField("_request", privateInstance);
                var closing = new UnityWebGetRequestTask(new Uri(textPath).AbsoluteUri, new RetryParam(1, 0, 0.01f), false, _ => calls++);
                var closingWait = closing.Task.GetAwaiter();
                closingWait.OnCompleted(() =>
                {
                    events.Add("await");
                    closing.Dispose();
                    requestSurvivedDispose = requestField.GetValue(closing) is UnityWebRequest request && request.downloadHandler != null;
                });
                system.AddTask(closing); await WaitDone(system, closing);
                Check(closing.State == TaskState.Succeeded && closing.Error == null && calls == 0
                    && events.SequenceEqual(new[] { "await" })
                    && System.Text.Encoding.UTF8.GetString((byte[])closingWait.GetResult()) == "result-delivery",
                    "Success-continuation cleanup still suppresses the pending callback without invalidating the data snapshot.");
                Check(requestSurvivedDispose && requestField.GetValue(closing) == null,
                    "WebGet keeps the current request alive through its completion stack, then releases it in finally.");

                events.Clear(); calls = 0; requestSurvivedDispose = false;
                var closingVersion = new LoadVersionPackTask(new Uri(packPath).AbsoluteUri, _ => calls++);
                var closingVersionWait = closingVersion.Task.GetAwaiter();
                closingVersionWait.OnCompleted(() =>
                {
                    events.Add("await");
                    closingVersion.Dispose();
                    requestSurvivedDispose = requestField.GetValue(closingVersion) is UnityWebRequest request && request.downloadHandler != null;
                });
                system.AddTask(closingVersion); await WaitDone(system, closingVersion);
                Check(closingVersion.State == TaskState.Succeeded && closingVersion.Error == null && calls == 0
                    && events.SequenceEqual(new[] { "await" }) && closingVersionWait.GetResult() is VersionPackInfo { Version: "result-delivery" }
                    && requestSurvivedDispose && requestField.GetValue(closingVersion) == null,
                    "VersionPack preserves its parsed result and existing callback suppression while request release waits for completion processing.");
            }
            finally
            {
                File.Delete(textPath);
                File.Delete(packPath);
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
                var requestField = typeof(UnityDownloadBaseTask).GetField("_request",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var blocker = new ProbeTask();
                system.AddTask(blocker); system.Tick(0);
                var calls = 0;
                byte[] delivered = null;
                var task = system.AddUnityWebGetRequestTask(new Uri(binaryPath).AbsoluteUri,
                    new RetryParam(1, 0, 0.01f), false, data => { calls++; delivered = data; });
                system.Tick(0);
                Check(task.State == TaskState.Pending && !task.IsExecuted && calls == 0 && requestField.GetValue(task) == null,
                    "The WebGet factory still waits for Default capacity before creating a request.");
                Check(system.RunningTaskCount == 1 && system.PendingTaskCount == 1,
                    "A queued snapshot request cannot bypass the existing quota.");
                blocker.Finish();
                await WaitDone(system, task);
                Check(task.State == TaskState.Succeeded && task.Error == null && requestField.GetValue(task) == null
                    && calls == 1 && delivered.SequenceEqual(expected),
                    "The factory delivers binary bytes unchanged and releases its request during retirement.");

                await UniTask.Yield();
                var late = (byte[])await task.Task;
                Check(ReferenceEquals(late, delivered) && ReferenceEquals(late, task.Result) && late.SequenceEqual(expected),
                    "The first await after cleanup and another frame returns the same readable snapshot.");

                var empty = system.AddUnityWebGetRequestTask(new Uri(emptyPath).AbsoluteUri,
                    new RetryParam(1, 0, 0.01f), false, callback: null);
                await WaitDone(system, empty);
                Check(empty.State == TaskState.Succeeded && empty.Error == null && empty.Result is byte[] { Length: 0 }
                    && requestField.GetValue(empty) == null, "An empty response succeeds with an empty array and a disposed request.");
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

        private sealed class ForeignTask : ITask
        {
            public int Observations;
            public float Progress { get { Observations++; return 0f; } }
            public bool IsExecuted { get { Observations++; return false; } }
            public bool IsDone { get { Observations++; return false; } }
            public object Result { get { Observations++; return null; } }
            public Exception Error { get { Observations++; return null; } }
            public TaskState State { get { Observations++; return TaskState.Pending; } }
            public UniTask<object> Task { get { Observations++; return UniTask.FromResult<object>(null); } }
            public void Cancel() { Observations++; }
            public bool MoveNext() { Observations++; return true; }
            public object Current { get { Observations++; return null; } }
            public void Reset() { Observations++; }
        }

        private sealed class ThrowingCurrent : IEnumerator, IDisposable
        {
            private readonly Exception _error;
            public int Disposals;
            public ThrowingCurrent(Exception error) { _error = error; }
            public bool MoveNext() => true;
            public object Current => throw _error;
            public void Reset() { }
            public void Dispose() { Disposals++; }
        }

        private sealed class DownloadProbe : UnityDownloadBaseTask
        {
            public Exception CreateError;
            public Exception CompletionError;
            public int Failures;
            public bool FailureWasPublished;
            public DownloadProbe(string uri) : base(uri, new RetryParam(1, 0, 0.01f), false) { }
            protected override UnityWebRequest CreateRequest()
            {
                if (CreateError != null) throw CreateError;
                return base.CreateRequest();
            }
            protected override void OnSuccess(UnityWebRequest request)
            {
                if (CompletionError != null) throw CompletionError;
                Complete(true);
            }
            protected override void OnFailed()
            {
                Failures++;
                FailureWasPublished = State == TaskState.Failed && Error != null;
            }
        }

        private static async UniTask FinalDisposal(TaskSystem system)
        {
            var active = new ProbeTask(); var pending = new ProbeTask(); var flow = new ProbeTask();
            system.AddTask(active); system.AddTask(pending); system.AddTask(flow, LiteConst.TaskGroupId.Flow);
            system.Tick(0); await system.DisposeAsync(); await system.DisposeAsync();
            Check(active.Disposals == 1 && pending.Disposals == 1 && flow.Disposals == 1, "Final disposal releases all registered tasks exactly once");
            Check(pending.Executions == 0 && active.IsDone && pending.IsDone && flow.IsDone, "Pending work is canceled without starting");
            Check(system.RunningTaskCount == 0 && system.PendingTaskCount == 0 && Snapshot(system).Count == 0, "Final disposal clears group totals and registration");
        }

        private sealed class ShutdownFixture
        {
            internal readonly List<string> Events = new();
            internal readonly List<string> Initialized = new();
            internal readonly Dictionary<string, ShutdownProbe> Instances = new();
            internal readonly UniTaskCompletionSource LogicGate = new();
            internal readonly UniTaskCompletionSource SystemGate = new();
            internal UniTaskCompletionSource<bool> InitializationGate;
            internal string FailedInitialization;
            internal bool ThrowOnInitialize;
            internal bool InTimerCallback;
            internal bool DisposalStartedInTimerCallback;
            internal int TicksAfterDispose;
        }

        private static ShutdownFixture _shutdownFixture;

        private abstract class ShutdownProbe : ILogic, ISystem
        {
            private readonly string _name;
            private readonly ShutdownFixture _fixture;
            private bool _disposed;
            internal int Disposals;

            protected ShutdownProbe(string name)
            {
                _name = name;
                _fixture = _shutdownFixture;
                _fixture.Instances.Add(name, this);
            }

            public async UniTask<bool> Initialize()
            {
                _fixture.Initialized.Add(_name);
                if (_name == _fixture.FailedInitialization)
                {
                    if (_fixture.ThrowOnInitialize)
                        throw new InvalidOperationException("InjectedValidation runtime initialization");
                    return false;
                }
                if (_name == "system 2" && _fixture.InitializationGate != null)
                    return await _fixture.InitializationGate.Task;
                return true;
            }

            public void Tick(float deltaTime)
            {
                if (_disposed) _fixture.TicksAfterDispose++;
            }

            public async UniTask DisposeAsync()
            {
                Disposals++;
                _fixture.DisposalStartedInTimerCallback |= _fixture.InTimerCallback;
                _fixture.Events.Add(_name + " start");
                if (_name == "logic 1") await _fixture.LogicGate.Task;
                if (_name == "system 2") await _fixture.SystemGate.Task;
                _disposed = true;
                _fixture.Events.Add(_name + " end");
            }
        }

        private sealed class ShutdownSystemOne : ShutdownProbe
        {
            public ShutdownSystemOne() : base("system 1") { }
        }

        private sealed class ShutdownSystemTwo : ShutdownProbe
        {
            public ShutdownSystemTwo() : base("system 2") { }
        }

        private sealed class ShutdownLogicOne : ShutdownProbe
        {
            public ShutdownLogicOne() : base("logic 1") { }
        }

        private sealed class ShutdownLogicTwo : ShutdownProbe
        {
            public ShutdownLogicTwo() : base("logic 2") { }
        }

        private static async UniTask BeginShutdownFixture(ShutdownFixture fixture)
        {
            var runtime = LiteRuntime.Instance;
            if (runtime.State != LiteRuntime.RuntimeState.Idle) await runtime.Shutdown();

            _shutdownFixture = fixture;
            var setting = new LiteSetting();
            setting.Asset.AssetMode = AssetProviderMode.Editor;
            setting.SystemList.Add(new LiteTypeEntryData<ISystem> { AssemblyQualifiedName = typeof(ShutdownSystemOne).AssemblyQualifiedName });
            setting.SystemList.Add(new LiteTypeEntryData<ISystem> { AssemblyQualifiedName = typeof(ShutdownSystemTwo).AssemblyQualifiedName });
            setting.LogicList.Add(new LiteTypeEntryData<ILogic> { AssemblyQualifiedName = typeof(ShutdownLogicOne).AssemblyQualifiedName });
            setting.LogicList.Add(new LiteTypeEntryData<ILogic> { AssemblyQualifiedName = typeof(ShutdownLogicTwo).AssemblyQualifiedName });
            runtime.Startup(runtime.Launcher, setting);
        }

        private static async UniTask WaitForRuntimeCondition(Func<bool> condition, string operation)
        {
            var deadline = Time.realtimeSinceStartup + 10f;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(operation);
                // The runner disables Launcher.Update; keep the real Runtime driving its systems.
                LiteRuntime.Instance.Tick(0f);
                await UniTask.NextFrame();
            }
        }

        private static async UniTask FinishShutdownFixture(ShutdownFixture fixture, UniTask? shutdown = null)
        {
            try
            {
                fixture.InitializationGate?.TrySetResult(true);
                fixture.LogicGate.TrySetResult();
                fixture.SystemGate.TrySetResult();

                var runtime = LiteRuntime.Instance;
                if (!shutdown.HasValue && runtime.State is not LiteRuntime.RuntimeState.Idle
                    and not LiteRuntime.RuntimeState.ShuttingDown and not LiteRuntime.RuntimeState.ShutdownFailed)
                    shutdown = runtime.Shutdown();

                if (shutdown.HasValue)
                {
                    var pending = shutdown.Value;
                    await WaitForRuntimeCondition(() => pending.Status != UniTaskStatus.Pending, "Fixture shutdown completion");
                    await pending;
                }
                else
                {
                    await WaitForRuntimeCondition(() => runtime.State != LiteRuntime.RuntimeState.ShuttingDown, "Automatic failure cleanup");
                }

                if (runtime.State != LiteRuntime.RuntimeState.Idle)
                    throw new Exception("Fixture cleanup did not finish in Idle: " + runtime.State);
            }
            finally
            {
                _shutdownFixture = null;
            }
        }

        private static async UniTask RuntimeShutdownOrder(TaskSystem system)
        {
            var fixture = new ShutdownFixture();
            UniTask? shutdown = null;
            try
            {
                await BeginShutdownFixture(fixture);
                await WaitForRuntimeCondition(() => LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Running, "Probe startup");
                Check(fixture.Initialized.SequenceEqual(new[] { "system 1", "system 2", "logic 1", "logic 2" }) &&
                    LiteRuntime.Get<ShutdownSystemOne>() != null && LiteRuntime.Get<ShutdownSystemTwo>() != null,
                    "Probes initialize through Runtime configuration and publish their systems.");

                shutdown = LiteRuntime.Instance.Shutdown();
                Check(fixture.Events.Count == 0 && shutdown.Value.Status == UniTaskStatus.Pending,
                    "Shutdown defers disposal until the initialization callback stack has exited.");
                await WaitForRuntimeCondition(() => fixture.Events.Count > 0, "First logic disposal");
                Check(fixture.Events.SequenceEqual(new[] { "logic 1 start" }) && shutdown.Value.Status == UniTaskStatus.Pending,
                    "Runtime shutdown waits for the first asynchronous logic before starting later cleanup.");
                await UniTask.NextFrame();
                Check(fixture.Events.SequenceEqual(new[] { "logic 1 start" }) && shutdown.Value.Status == UniTaskStatus.Pending,
                    "The pending logic continues to hold shutdown across a player loop.");

                LiteRuntime.Instance.EnterErrorState(FrameworkErrorCode.Unknown);
                Check(LiteRuntime.Instance.State == LiteRuntime.RuntimeState.ShuttingDown,
                    "Reporting an error does not overwrite the active shutdown state.");

                LiteRuntime.Instance.IsPause = true;
                LiteRuntime.Setting.Common.TimeScale = 0f;
                LiteRuntime.Timer.NextFrame(() =>
                {
                    fixture.InTimerCallback = true;
                    try { fixture.LogicGate.TrySetResult(); }
                    finally { fixture.InTimerCallback = false; }
                });
                LiteRuntime.Instance.Tick(0f);
                Check(fixture.Events.SequenceEqual(new[] { "logic 1 start", "logic 1 end" }),
                    "Paused shutdown still drives Timer, but does not start the next disposal inside its callback.");

                await WaitForRuntimeCondition(() => fixture.Events.Contains("system 2 start"), "First system disposal");
                Check(fixture.Events.SequenceEqual(new[] { "logic 1 start", "logic 1 end", "logic 2 start", "logic 2 end", "system 2 start" }) && shutdown.Value.Status == UniTaskStatus.Pending,
                    "Logic finishes in registration order before reverse-order system disposal begins.");
                await UniTask.NextFrame();
                Check(fixture.Events.Count == 5 && shutdown.Value.Status == UniTaskStatus.Pending,
                    "The pending system holds shutdown across a player loop.");

                LiteRuntime.Timer.NextFrame(() =>
                {
                    fixture.InTimerCallback = true;
                    try { fixture.SystemGate.TrySetResult(); }
                    finally { fixture.InTimerCallback = false; }
                });
                LiteRuntime.Instance.Tick(0f);
                Check(fixture.Events.Count == 6 && LiteRuntime.Get<ShutdownSystemTwo>() == null &&
                    !fixture.DisposalStartedInTimerCallback && fixture.TicksAfterDispose == 0,
                    "A disposed system is unpublished and skipped by the remaining Tick before the next system is released.");
            }
            finally
            {
                await FinishShutdownFixture(fixture, shutdown);
            }

            Check(fixture.Events.SequenceEqual(new[] { "logic 1 start", "logic 1 end", "logic 2 start", "logic 2 end", "system 2 start", "system 2 end", "system 1 start", "system 1 end" }) &&
                LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Idle,
                "Shutdown finishes the remaining systems in reverse order before returning to Idle.");
            Check(fixture.Instances.Values.All(probe => probe.Disposals == 1) && fixture.TicksAfterDispose == 0,
                "Every probe is disposed exactly once and no released probe is ticked.");
        }

        private static async UniTask RuntimeInitializationReturnsFalse(TaskSystem system)
        {
            var fixture = new ShutdownFixture { FailedInitialization = "system 2" };
            fixture.SystemGate.TrySetResult();
            try
            {
                await BeginShutdownFixture(fixture);
                await WaitForRuntimeCondition(() => LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Idle, "False initialization cleanup");
                Check(fixture.Initialized.SequenceEqual(new[] { "system 1", "system 2" }) && fixture.Instances.Count == 2,
                    "A false system initialization prevents later logic construction.");
                Check(fixture.Events.SequenceEqual(new[] { "system 2 start", "system 2 end", "system 1 start", "system 1 end" }) &&
                    fixture.Instances.Values.All(probe => probe.Disposals == 1),
                    "The unpublished failed system and previously initialized system are both disposed exactly once.");
                Check(LiteRuntime.Get<ShutdownSystemOne>() == null && LiteRuntime.Get<ShutdownSystemTwo>() == null,
                    "Failed initialization leaves no published probe system after cleanup.");
            }
            finally { await FinishShutdownFixture(fixture); }
        }

        private static async UniTask RuntimeInitializationThrows(TaskSystem system)
        {
            var fixture = new ShutdownFixture { FailedInitialization = "logic 2", ThrowOnInitialize = true };
            fixture.LogicGate.TrySetResult();
            fixture.SystemGate.TrySetResult();
            try
            {
                await BeginShutdownFixture(fixture);
                await WaitForRuntimeCondition(() => LiteRuntime.Instance.State == LiteRuntime.RuntimeState.Idle, "Throwing initialization cleanup");
                Check(fixture.Initialized.SequenceEqual(new[] { "system 1", "system 2", "logic 1", "logic 2" }),
                    "The throwing logic is created through the real initialization path.");
                Check(fixture.Events.SequenceEqual(new[] { "logic 2 start", "logic 2 end", "logic 1 start", "logic 1 end", "system 2 start", "system 2 end", "system 1 start", "system 1 end" }),
                    "Unpublished failed logic is cleaned before ready logic and reverse-order systems.");
                Check(fixture.Instances.Values.All(probe => probe.Disposals == 1),
                    "Initialization exceptions do not omit or duplicate disposal.");
            }
            finally { await FinishShutdownFixture(fixture); }
        }

        private static async UniTask RuntimeShutdownDuringInitialization(TaskSystem system)
        {
            var fixture = new ShutdownFixture { InitializationGate = new UniTaskCompletionSource<bool>() };
            fixture.SystemGate.TrySetResult();
            UniTask? shutdown = null;
            try
            {
                await BeginShutdownFixture(fixture);
                Check(LiteRuntime.Instance.State == LiteRuntime.RuntimeState.InitSystem &&
                    fixture.Initialized.Contains("system 2") && LiteRuntime.Get<ShutdownSystemTwo>() == null,
                    "The initializing system is owned but not published.");

                shutdown = LiteRuntime.Instance.Shutdown();
                await UniTask.NextFrame();
                Check(fixture.Events.Count == 0 && shutdown.Value.Status == UniTaskStatus.Pending,
                    "Shutdown does not dispose an object while its Initialize is still executing.");
                LiteRuntime.Instance.IsPause = true;
                LiteRuntime.Setting.Common.TimeScale = 0f;
                LiteRuntime.Timer.NextFrame(() => fixture.InitializationGate.TrySetResult(true));
                LiteRuntime.Instance.Tick(0f);
                Check(fixture.Events.Count == 0 && LiteRuntime.Get<ShutdownSystemTwo>() == null,
                    "A late successful Initialize is not published after shutdown cancellation, and cleanup stays outside the callback.");
            }
            finally { await FinishShutdownFixture(fixture, shutdown); }

            Check(fixture.Initialized.SequenceEqual(new[] { "system 1", "system 2" }) &&
                fixture.Events.SequenceEqual(new[] { "system 2 start", "system 2 end", "system 1 start", "system 1 end" }) &&
                fixture.Instances.Values.All(probe => probe.Disposals == 1),
                "In-flight initialization exits before exactly-once cleanup and no logic initialization follows.");
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

        private static IEnumerator EmptyRoutine() { yield break; }

        private static IEnumerator ParentRoutine(TaskSystem system, ProbeTask child, Action resumed)
        {
            system.AddTask(child);
            yield return child;
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

        private sealed class ControlledResult : IAsyncResult
        {
            private bool _isCompleted;
            public Action OnRead;
            public bool IsCompleted
            {
                get { OnRead?.Invoke(); return _isCompleted; }
                set { _isCompleted = value; }
            }
            public bool CompletedSynchronously => false;
            public object AsyncState => null;
            public WaitHandle AsyncWaitHandle => throw new NotSupportedException();
        }
    }
}
