# TaskSystem 任务调度

`LiteRuntime.Task` 调度协程、Unity 异步操作和回调任务，也提供主线程投递。只调度显式提交的任务，可与直接使用 UniTask 并存。

## 提交、等待与取消

示例使用 `LiteQuark.Runtime`、`Cysharp.Threading.Tasks` 命名空间，在框架初始化后的异步方法中调用：

```csharp
var task = new WaitCallbackTask(done => done(true));
LiteRuntime.Task.AddTask(task, priority: TaskPriority.High);
await task.Task;
if (task.State == TaskState.Failed)
{
    LLog.Exception(task.Error);
}
```

只提交新的 Pending 任务。`BaseTask` 用于实现和提交，`ITask` 用于观察、等待和取消。提交后由 TaskSystem 推进并清理任务；资源 Handle 仍由调用方释放。

| 入口 | 默认组 | 用途 |
| --- | --- | --- |
| `AddTask(AsyncOperation, callback)`、`AddTask(IAsyncResult, callback)` | Flow | 观察已经启动的操作 |
| `AddMainThreadTask(func, param)` | Flow | 排队执行主线程委托 |
| `AddTask(IEnumerator, callback)` | Default | 启动协程 |
| `AddTask(Action<Action<bool>>)` | Default | 包装回调式操作，done(false) 表示失败 |
| `AddInstantiateGoTask(...)`、`AddUnityWebGetRequestTask(...)` | Default | 异步实例化／下载 |

便利入口使用 Normal。指定组或优先级时构造 `BaseTask` 派生实例，再传给 `AddTask(task, groupId: ..., priority: ...)`，省略组时使用 Default。

在异步方法中用 `await task.Task`；在协程中先提交，再用 `yield return task.Task.ToCoroutine()`。`ToCoroutine()` 只等待终态，不传播失败或接管取消；父协程取消时仍需按需取消该任务。

## 状态、结果与通知

| 状态 | 结果和等待语义 |
| --- | --- |
| Pending / Running | 等待准入／正在执行 |
| Succeeded | 最终结果允许为 null，Progress 为 1 |
| Failed | Error 为非空异常，Result 为 null |
| Cancelled | Error 和 Result 均为 null |

`IsDone` 覆盖三个不可逆的终态。`Cancel()` 立即使未终结的任务进入 Cancelled，底层原生操作可能仍在进行。`Task` 为 `UniTask<object>`，**失败与取消均返回 `null`，不会因此抛异常**。检查 `State` / `Error`，不要用结果或进度判断成功。任务清理不会清空已发布的结果；结果不代表独立的 Unity 资源所有权。

框架提供的完成回调至多通知一次，取消或关闭时不保证通知；协程取消不会调用成功回调。`await` 只等待逻辑终态，回调与续体的先后因任务而异，续体可能同步重入。回调异常会记录，不改变终态。异步实例化失败时，回调收到空数组，`Result` 为 `null`。

`AddUnityWebGetRequestTask` 成功时返回 `byte[]` 快照（空响应返回空数组），回调和等待共享同一数组；按只读使用，无需 Dispose。失败回调收到 `null`，取消不发回调。

## 分组与并发

| 保留组常量（LiteConst.TaskGroupId） | ID | 额度 |
| --- | --- | --- |
| Default | 0 | Launcher 的 Task.ConcurrencyLimit，默认 20，构造时限制为至少 1 |
| Flow | -1 | null，不限制准入数量 |
| Asset | -2 | AssetSystem 注册；配置与加载行为见 [Asset 指南](../../../Asset/README.md#加载优先级) |

任务组独立计数，业务组用其他 ID，额度为正整数或 `null`（无限额）。不支持重复注册、提交到未注册组、运行时改额度、删除组或迁组。

```csharp
const int workGroupId = 1; // 项目统一约定，初始化阶段注册一次。
LiteRuntime.Task.RegisterGroup(workGroupId, concurrencyLimit: 2);
var task = new WaitCallbackTask(done => done(true));
LiteRuntime.Task.AddTask(task, groupId: workGroupId, priority: TaskPriority.High);
```

父任务占满同组额度后等待同组子任务，可能卡住。仅编排或等待的任务可放入 Flow；直接等待 UniTask / Handle 不占额度。额度统计已准入、尚未收尾的逻辑任务，Flow 也不保证立即执行。`RunningTaskCount` / `PendingTaskCount` 统计所有组；`StartCoroutine()` / `StopCoroutine()` 不经过队列。

## 优先级与执行时序

`TaskPriority` 的 Low / Normal / High / Urgent 只决定同组启动顺序，同级 FIFO。提交后优先级固定；不抢占、不绕过额度，也不保证完成顺序，持续高优先级任务可能推迟低优先级任务。

`AddTask` 只登记；调度时先推进和收尾，再启动待执行任务。新启动的任务下一轮才收到 Tick。组之间没有启动顺序或帧耗时预算保证。

## 自定义任务

继承 [BaseTask](Base/BaseTask.cs)，构造阶段只保存参数，需释放的资源在 OnExecute 中取得。通过受保护的 Complete / Fail 发布结果，不自行改写终态。

| 扩展点 | 责任 |
| --- | --- |
| `OnExecute()` | 启动工作；成功调用 Complete(result)，失败调用 Fail(exception) |
| `OnTick(deltaTime)` | 可选的逐帧推进 |
| `OnTerminated()` | 终态已确定时停止操作或发送终态通知；此时 IsDone 已为 true，不能因此跳过清理 |
| `OnDispose()` | 调度器移除任务时释放包装器资源，不改写已发布的 Result / Error |

`Complete` 仅在 Running 时接受结果；`Fail(null)` 抛参数异常。`OnTerminated` 在结束等待前执行，清理异常会记录，不覆盖已发布结果或触发重试。

调度器将 Execute / Tick 异常转为 Failed。自定义回调应先回到主线程，再通过 `Fail` 报告执行异常，并负责处置取消后迟到的结果。

## 主线程与关闭

- `PostMainThreadTask(callback, state)` 异步投递到初始化时捕获的同步上下文。
- `SendMainThreadTask(callback, state)` 同步发送，可能阻塞调用线程。
- `AddMainThreadTask(func, param)` 加入 Flow，由后续调度执行。

在主线程访问 Unity 对象、修改队列和取消任务。关闭时停止提交；系统停止 Launcher 上的协程并取消、清理任务。资源与场景的退场见[资源系统关闭](../../../Asset/README.md#系统关闭)。

完整接口见 [TaskSystem.cs](TaskSystem.cs)，升级步骤见 [CHANGELOG](../../../../CHANGELOG.md)。
