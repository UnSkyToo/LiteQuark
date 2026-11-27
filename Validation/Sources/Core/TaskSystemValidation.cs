using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal static partial class CorePlayModeValidationRunner
    {
        private static UniTask RunTaskCase(string name, Action<TaskSystem> test, params string[] expectedErrors)
        {
            return RunTaskCaseAsync(name, system => { test(system); return UniTask.CompletedTask; }, expectedErrors);
        }

        private static UniTask RunTaskCaseAsync(string name, Func<TaskSystem, UniTask> test, params string[] expectedErrors)
        {
            var originalLimit = LiteRuntime.Setting.Task.ConcurrencyLimit;
            TaskSystem system = null;
            return RunCaseAsync(name, async _ =>
            {
                LiteRuntime.Setting.Task.ConcurrencyLimit = 1;
                system = new TaskSystem();
                LiteRuntime.Setting.Task.ConcurrencyLimit = originalLimit;
                await system.Initialize();
                await test(system);
            }, async () =>
            {
                LiteRuntime.Setting.Task.ConcurrencyLimit = originalLimit;
                if (system != null) await system.DisposeAsync();
            }, expectedErrors);
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
            var early = failed.WaitForCompletionAsync().GetAwaiter();
            failed.TerminalAction = task => Check(task.State == TaskState.Failed && ReferenceEquals(task.Error, error)
                && task.Result == null && !early.IsCompleted, "The termination hook sees the failure snapshot before existing waiters resume.");
            failed.Fail(error);
            failed.Cancel(); failed.Fail(new Exception("Late error")); failed.Finish(); failed.Dispose();
            Check(failed.Terminations == 1 && failed.State == TaskState.Failed && ReferenceEquals(failed.Error, error)
                && !failed.IsExecuted, "Pending failure is irreversible and survives Dispose.");
            Check(early.IsCompleted, "Failure finishes an existing wait.");
            early.GetResult();
            failed.WaitForCompletionAsync().GetAwaiter().GetResult();

            var executionError = new InvalidOperationException("Validation execution failure");
            var throwing = new ProbeTask { ExecuteAction = _ => throw executionError };
            system.AddTask(throwing);
            system.Tick(0);
            Check(throwing.State == TaskState.Failed && ReferenceEquals(throwing.Error, executionError) &&
                throwing.Disposals == 1, "Scheduler propagates execution failure and releases the occupied slot.");

            var late = new ProbeTask(); late.Fail(error);
            var lateWait = late.WaitForCompletionAsync().GetAwaiter();
            Check(lateWait.IsCompleted && ReferenceEquals(late.Error, error),
                "A wait first requested after failure completes immediately with the original error available on the task.");
            lateWait.GetResult();
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
            canceled.WaitForCompletionAsync().GetAwaiter().GetResult();
            Check(canceled.State == TaskState.Cancelled && canceled.Error == null && canceled.Result == null
                && canceled.Terminations == 1, "Cancellation is distinct from failure and remains final.");
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
                var earlyWait = success.WaitForCompletionAsync().GetAwaiter();
                earlyWait.OnCompleted(() => success.Dispose());
                system.AddTask(success); await WaitDone(system, success);
                Check(success.State == TaskState.Succeeded && success.Error == null && callbacks == 1
                    && success.Result is GameObject[] { Length: 2 }, "Native success preserves result after scheduler disposal.");
                earlyWait.GetResult();
                Check(((GameObject[])success.Result).All(go => go != null),
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
                await task.WaitForCompletionAsync();
                Check(task.Result is byte[] late && ReferenceEquals(late, delivered) && late.SequenceEqual(expected),
                    "The same snapshot remains readable after cleanup, another frame, and a late wait.");

                var empty = system.AddUnityWebGetRequestTask(new Uri(emptyPath).AbsoluteUri,
                    new RetryParam(1, 0, 0.01f), false, callback: null);
                await WaitDone(system, empty);
                Check(empty.State == TaskState.Succeeded && empty.Error == null && empty.Result is byte[] { Length: 0 },
                    "An empty response succeeds with an empty array.");
                var absent = system.AddUnityWebGetRequestTask(new Uri(Path.Combine(scratch, "task-snapshot-missing.bin")).AbsoluteUri,
                    new RetryParam(1, 0, 0.01f), false, callback: null);
                await WaitDone(system, absent);
                await absent.WaitForCompletionAsync();
                Check(absent.State == TaskState.Failed && absent.Error != null && absent.Result == null,
                    "A missing file reports failure rather than an empty successful snapshot.");
                await empty.WaitForCompletionAsync();
                Check(empty.Result is byte[] { Length: 0 }
                    && Snapshot(system).Count == 0 && system.RunningTaskCount == 0 && system.PendingTaskCount == 0,
                    "An unobserved empty success remains available to a late await without retaining a scheduler slot.");
            }
            finally
            {
                File.Delete(binaryPath);
                File.Delete(emptyPath);
            }
        }

        private static async UniTask WebGetRetries(TaskSystem system)
        {
            const float retryInterval = 1f;
            var runtime = LiteRuntime.Instance;
            var originalPause = runtime.IsPause;
            var originalScale = Time.timeScale;
            var scenarios = new (string Path, Action<UnityWebGetRequestTask> StopRetry, string Evidence)[]
            {
                ("limit", null, "One retry is sent while paused, then failure is published exactly once."),
                ("cancel", task => task.Cancel(), "Cancelling during the wait prevents the retry and its callback."),
                ("error", _ => runtime.EnterErrorState(FrameworkErrorCode.Unknown),
                    "A retry due in Error cancels the task, completes its existing wait, and sends no request or callback.")
            };
            foreach (var (path, stopRetry, evidence) in scenarios)
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                UnityWebGetRequestTask task = null;
                try
                {
                    listener.Start();
                    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                    var url = $"http://127.0.0.1:{port}/retry/{path}";
                    var callbacks = 0;
                    task = system.AddUnityWebGetRequestTask(url, new RetryParam(1, 1, retryInterval),
                        false, _ => callbacks++);
                    system.Tick(0f);
                    runtime.IsPause = true;
                    Time.timeScale = 0f;

                    // Holding both sockets open without a response triggers real request timeouts.
                    await WaitForRuntimeCondition(listener.Pending, "Initial download connection");
                    using var first = listener.AcceptTcpClient();
                    await WaitForRuntimeCondition(() => _current.evidence.Any(line => line.Contains(url)), "Initial download timeout");
                    Check(task.State == TaskState.Running && system.RunningTaskCount == 1 && callbacks == 0,
                        "Waiting to retry retains the download slot and defers the callback.");

                    var completion = task.WaitForCompletionAsync();
                    if (stopRetry != null)
                    {
                        stopRetry(task);
                    }
                    else
                    {
                        await WaitForRuntimeCondition(listener.Pending, "Retry while Runtime is paused and time scale is zero");
                        using var retry = listener.AcceptTcpClient();
                        await WaitDone(system, task);
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(retryInterval + 0.25f), DelayType.Realtime);
                    system.Tick(0f);
                    Check(!listener.Pending() && system.RunningTaskCount == 0 &&
                          completion.Status == UniTaskStatus.Succeeded && task.Result == null &&
                          (stopRetry != null ? task.State == TaskState.Cancelled && callbacks == 0
                                             : task.State == TaskState.Failed && task.Error != null && callbacks == 1), evidence);
                    await completion;
                }
                finally
                {
                    task?.Cancel();
                    listener.Stop();
                    runtime.IsPause = originalPause;
                    Time.timeScale = originalScale;
                    if (runtime.State == LiteRuntime.RuntimeState.Error)
                    {
                        await runtime.Shutdown();
                        runtime.Startup(runtime.Launcher);
                        await WaitForRuntimeCondition(() => runtime.State == LiteRuntime.RuntimeState.Running, "Startup after download Error case");
                    }
                }
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
            yield return view.WaitForCompletionAsync().ToCoroutine();
            resumed();
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
            var completion = successor.WaitForCompletionAsync();
            await system.DisposeAsync();
            Check(completion.Status == UniTaskStatus.Succeeded && successor.Result == null &&
                successor.State == TaskState.Cancelled && successor.Disposals == 1, "Shutdown settles the accepted task and releases it once.");
            await completion;
        }
    }
}
