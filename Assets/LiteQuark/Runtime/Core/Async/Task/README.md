# TaskSystem 任务调度

`LiteRuntime.Task` 调度协程、Unity 异步操作和回调任务，并提供主线程投递。任务提交到指定组，各组拥有独立并发额度和稳定优先队列；系统没有跨组的总并发上限。它与直接使用 UniTask 的异步方法并存，只有加入 TaskSystem 的任务受其调度。

## 创建与等待

在异步方法中包装回调式操作，并在提交时指定优先级：

```csharp
var task = new WaitCallbackTask(done =>
{
    // 在实际异步操作结束时调用 done(true)，失败时调用 done(false)。
    done(true);
});
LiteRuntime.Task.AddTask(task, priority: TaskPriority.High);
await task.Task;
if (task.State == TaskState.Cancelled)
{
    LLog.Warning("Task cancelled");
}
```

`AddTask` 的便利重载支持 `IEnumerator`、`UnityEngine.AsyncOperation`、`IAsyncResult` 和 `Action<Action<bool>>`；前三种包装还接受完成回调。便利入口的签名保持不变，公开便利入口使用 `Normal` 优先级，按以下规则选择组：

| 入口 | 使用的组 | 含义 |
| --- | --- | --- |
| `AddTask(AsyncOperation, callback)`、`AddTask(IAsyncResult, callback)` | Flow | 观察已有操作，不等待默认组空位 |
| `AddMainThreadTask(func, param)` | Flow | 排队派发主线程委托，不等待默认组空位 |
| `AddTask(IEnumerator, callback)`、`AddTask(Action<Action<bool>>)` | Default | 包装内容可能启动实际工作，不自动视为流程 |
| `AddInstantiateGoTask(...)`、`AddUnityWebGetRequestTask(...)` | Default | 启动实际实例化或请求操作 |

需要指定组或优先级时，先构造任务对象，再调用 `AddTask(ITask task, int groupId = DefaultGroupId, TaskPriority priority = TaskPriority.Normal)`。通用入口只按传入的组登记，不根据任务类型自动分组；例如自行构造 `AsyncOperationTask` 后通过该入口提交，若不指定组仍进入 Default。

自定义任务一般继承 `BaseTask`，实现 `OnExecute()`、`Dispose()`，按需重写 `OnTick()`，结束时调用 `Complete(result)`。任务是一次性对象：只提交尚未执行的 Pending 任务，不重复提交、跨组重复提交或在外部调用其 `Execute()`。成功提交后，由 TaskSystem 负责最终释放。

状态为 `Pending → InProgress → Completed`，也可以进入 `Cancelled`。`IsDone` 同时包含完成和取消。`BaseTask.Task` 的类型是 `UniTask<object>`；取消使它以 null 完成，不会抛出取消异常，因此需要检查 `State`。TaskSystem 移除终态任务时自动调用 `Dispose()`。

## 分组与并发

在 Launcher 的 Task 设置中配置：

| 设置 | 默认值 | 作用 |
| --- | --- | --- |
| `ConcurrencyLimit` | 20 | 默认组的并发上限，构造 TaskSystem 时读取并限制为至少 1 |

TaskSystem 自动注册 Default 和 Flow 两个组：

| 组 | ID | 额度 |
| --- | --- | --- |
| Default | `TaskSystem.DefaultGroupId`（0） | 使用 Launcher 的 Task 设置 |
| Flow | `TaskSystem.FlowGroupId`（`int.MaxValue`） | `null`，没有数量准入上限 |

两个内置 ID 已被占用，不需要再次注册。其他组由调用方注册；有额度组要求正整数，`null` 表示无限额组。组 ID 不可重复，提交到未注册的组会报错；当前不提供运行时更改额度、删除组或任务迁组接口。

```csharp
// 在初始化阶段注册一次；组 ID 由项目统一约定。
const int workGroupId = 1;
LiteRuntime.Task.RegisterGroup(workGroupId, concurrencyLimit: 2);

var task = new WaitCallbackTask(done => done(true));
LiteRuntime.Task.AddTask(task, groupId: workGroupId, priority: TaskPriority.High);
```

组表示共享额度，尚未将 Asset 等模块划分到独立业务组。Flow 中的任务仍统一登记、推进和收尾，也计入系统运行统计，但不消耗 Default 的额度。Flow 不代表立即执行，也不限制每帧 CPU 耗时。

同组父任务占满额度后等待同组子任务，仍可能无法继续。调度器不会识别协程或回调内部的等待关系；仅编排子任务或等待结果的流程，应在构造任务后显式传入 `groupId: TaskSystem.FlowGroupId`。实际启动下载、计算等工作的协程或回调任务，仍应使用有额度的工作组。直接使用 UniTask 或 Handle 的等待流程本来就不占 TaskSystem 额度，无需为分组再包装成 Task。独立组也不能消除依赖循环。

额度统计的是已准入、尚未收尾的逻辑任务。取消任务后，底层 Unity 操作不一定已经停止；包装已有的 `AsyncOperation` / `IAsyncResult` 也不会延迟底层操作启动。因此组额度不等于底层操作的严格物理并发上限。

`RunningTaskCount` / `PendingTaskCount` 是所有组（包括 Flow）的运行和排队数量之和，只用于观测，不作为总准入上限。直接 `StartCoroutine()` / `StopCoroutine()` 使用 Launcher 承载协程，不走分组队列。

## 优先级与执行时序

`TaskPriority` 依次为 `Low`、`Normal`、`High`、`Urgent`，只决定同组待执行任务的启动顺序；同级按入队顺序执行。优先级保存在调度记录中，提交后不可修改，任何级别都不会绕过组额度。运行任务不抢占，启动顺序不保证完成顺序；持续有高优先级任务时，低优先级任务可能饥饿。

`AddTask` 只登记，不立即调用 `Execute()`。每轮 Tick 先推进已有任务并收尾，再为各组补位；新启动任务从下一轮开始收到 `Tick()`，同步完成会立即归还额度。回调中提交的新任务可能参与本轮补位，但新注册的组从下一轮开始参与调度。每组本轮准入尝试次数以该组开始补位时的待执行数量为限，避免同步任务无限派生；这不是每帧耗时预算，也不承诺跨组启动顺序。

Asset 模块的 `AssetLoadPriority` 用于 Bundle Loader 自己的稳定优先队列，与这里的 `TaskPriority` 独立。版本包、本地和远程 Bundle 任务在创建入口以 `Urgent` 提交到默认组；Bundle 就绪后的 Asset/Scene 任务仍以 `Normal` 提交到默认组。这些任务共享默认组额度，不再享有绕限权限。Bundle Loader 仍保留自己的队列和并发限制，Asset 请求提权不会修改已提交任务的调度优先级，也不保证整条加载链优先。详见 [资源加载优先级](../../../Asset/README.md#加载优先级)。

## 主线程

```csharp
LiteRuntime.Task.PostMainThreadTask(_ => LLog.Info("Main thread"), null);
```

- `PostMainThreadTask(callback, state)` 投递到初始化时捕获的同步上下文。
- `SendMainThreadTask(callback, state)` 同步发送，可能阻塞调用线程。
- `AddMainThreadTask(func, param)` 加入 TaskSystem 的 Flow 组，由后续调度执行，与投递接口不同。
- 不要在后台线程访问 Unity 对象或直接修改任务队列；需要同步时使用上述投递入口。避免主线程等待正在等待主线程的工作。

TaskSystem 由框架在主线程驱动；不递归调用 Tick，不在任务回调中释放 TaskSystem。框架停止调度后调用 Dispose，清理回调不再提交任务。关闭时停止 Launcher 上的协程，并取消、释放全部登记任务。资源加载使用 AssetSystem 的 Handle API，不直接依赖内部加载任务。

签名见 [TaskSystem.cs](TaskSystem.cs)，基础状态实现见 [BaseTask.cs](Base/BaseTask.cs)。
