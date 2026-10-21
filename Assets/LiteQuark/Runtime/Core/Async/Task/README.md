# TaskSystem 任务调度

`LiteRuntime.Task` 调度协程、Unity 异步操作和回调任务，并提供主线程投递。它与直接使用 UniTask 的异步方法并存；只有加入 TaskSystem 的任务受其并发限制。

## 创建与等待

在异步方法中包装回调式操作：

```csharp
var task = LiteRuntime.Task.AddTask(done =>
{
    // 在实际异步操作结束时调用 done(true)，失败时调用 done(false)。
    done(true);
});
task.SetPriority(TaskPriority.High);
await task.Task;
if (task.State == TaskState.Cancelled)
{
    LLog.Warning("Task cancelled");
}
```

`AddTask` 支持 `IEnumerator`、`UnityEngine.AsyncOperation`、`IAsyncResult`、`Action<Action<bool>>` 和自定义 `ITask`。前三种包装还接受完成回调。自定义任务一般继承 `BaseTask`，实现 `OnExecute()`、`Dispose()`，按需重写 `OnTick()`，结束时调用 `Complete(result)`。

状态为 `Pending → InProgress → Completed`，也可以进入 `Cancelled`。`IsDone` 同时包含完成和取消。`BaseTask.Task` 的类型是 `UniTask<object>`；取消使它以 null 完成，不会抛出取消异常，因此需要检查 `State`。TaskSystem 移除终态任务时自动调用 `Dispose()`。

## 调度

在 Launcher 的 Task 设置中配置：

| 设置 | 默认值 | 作用 |
| --- | --- | --- |
| `ConcurrencyLimit` | 20 | 普通任务的并发上限，运行时至少为 1 |
| `IgnoreLimitPriority` | High | 达到该级别的任务绕过并发限制 |

优先级依次为 `Low`、`Normal`、`High`、`Urgent`。优先级用于判断是否绕过上限，队列并不按优先级排序；任务在后续 Tick 中执行，设置 High 不等于在 `AddTask` 内立即执行。配置在 TaskSystem 构造时读取。

通过 `RunningTaskCount` / `PendingTaskCount` 查看调度数量。直接 `StartCoroutine()` / `StopCoroutine()` 使用 Launcher 承载协程，不走该队列。

Asset 模块的 `AssetLoadPriority` 用于 Bundle Loader 自己的稳定优先队列，与这里的 `TaskPriority` 独立。Bundle Loader 限制自身并发，并将提交的 Bundle Task 设为 `Urgent`；这些任务绕过 TaskSystem 准入上限，但仍计入 `RunningTaskCount`。Bundle 就绪后的 Asset/Scene 加载任务仍按普通任务调度，因此 Asset 请求提权不保证整条加载链优先，也不会改变全局并发策略。详见 [资源加载优先级](../../../Asset/README.md#加载优先级)。

## 主线程

```csharp
LiteRuntime.Task.PostMainThreadTask(_ => LLog.Info("Main thread"), null);
```

- `PostMainThreadTask(callback, state)` 投递到初始化时捕获的同步上下文。
- `SendMainThreadTask(callback, state)` 同步发送，可能阻塞调用线程。
- `AddMainThreadTask(func, param)` 加入 TaskSystem 队列，与投递接口不同。
- 不要在后台线程访问 Unity 对象或直接修改任务队列；需要同步时使用上述投递入口。避免主线程等待正在等待主线程的工作。

TaskSystem 关闭时停止 Launcher 上的协程并取消、释放队列任务。资源加载使用 AssetSystem 的 Handle API，不直接依赖内部加载任务。

签名见 [TaskSystem.cs](TaskSystem.cs)，基础状态实现见 [BaseTask.cs](Base/BaseTask.cs)。
