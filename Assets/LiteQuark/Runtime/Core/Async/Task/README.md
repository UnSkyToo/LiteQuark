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
if (task.State == TaskState.Failed)
{
    LLog.Exception(task.Error);
}
else if (task.State == TaskState.Cancelled)
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

需要指定组或优先级时，先构造任务对象，再调用 `AddTask(ITask task, int groupId = LiteConst.TaskGroupId.Default, TaskPriority priority = TaskPriority.Normal)`。可提交的对象必须继承 `BaseTask`；独立实现 `ITask` 的对象会在登记前抛出 `ArgumentException`，不会占用队列或额度。保留 ITask 参数也使静态类型为 ITask 的调用继续匹配此入口，不会退回 IEnumerator 包装重载。通用入口只按传入的组登记，不根据任务类型自动分组；例如自行构造 `AsyncOperationTask` 后通过该入口提交，若不指定组仍进入 Default。

自定义任务继承 `BaseTask`，实现 `OnExecute()`，按需重写 `OnTick()`、`OnTerminated()` 和 `OnDispose()`，成功时调用受保护的 `Complete(result)`，失败时调用 `Fail(exception)`。构造阶段只保存参数；需显式释放的资源在 OnExecute 中取得，避免提交失败或提交前放弃时无人释放。任务是一次性对象：只提交尚未执行的 Pending 任务，不重复提交或跨组重复提交。成功提交后，由 TaskSystem 负责最终清理包装器。

`ITask` 只提供只读状态、结果、进度、UniTask 等待、IEnumerator 等待及公开的 `Cancel()`。它不再继承 ITick / IDispose，也不再公开 Execute / Fail。BaseTask 的 Execute、Tick、Dispose 为 internal，由框架调用；Fail 为 protected internal，供派生任务报告失败及调度器记录执行异常。框架程序集及友元程序集能够访问 internal 入口，不表示它们是业务扩展接口。

状态为 `Pending → Running → Succeeded`，Pending 或 Running 也可以进入 `Failed` / `Cancelled`。Succeeded 表示该任务按自身契约成功，结果允许为 null；Failed 的 `Error` 保存非空异常，取消的 `Error` 为 null。`IsDone` 包含这三个终态。`BaseTask.Task` 的类型是 `UniTask<object>`；失败和取消均以 null 完成，不会因这两种终态抛异常，因此需要检查 `State` / `Error`。终态前后取得 Task 的结果语义一致。成功将 Progress 设为 1，失败或取消保留最后进度；进度不能用来判断成功。

终态不可逆。`Complete(result)` 仅在 Running 时接受结果，并返回是否成功提交；重复 Complete/Fail/Cancel 不再通知。`Fail(null)` 抛出参数异常。State、Result、Error 由 BaseTask 维护，先写入完整终态，再执行一次 `OnTerminated()` 并结束已有等待。该钩子用于停止操作和终态通知，不承担尚未完成的结果计算。TaskSystem 移除终态任务时调用内部 Dispose，再调用 OnDispose；清理标记在钩子调用前设置，重复或重入清理只尝试一次，清理抛异常也不自动重试。子类释放包装器资源时不得改写已发布的结果和错误；Dispose 不隐式取消任务。终态通知或清理的异常单独记录，不覆盖原始失败。

Execute/Tick 的异常由 TaskSystem 转为 Failed，并在收尾前归还调度额度。异步原生回调、重试定时回调中的执行异常由对应任务捕获；执行主体不能用 `SafeInvoke` 吞掉异常，已经结束后的观察回调则可以隔离异常。`WaitCallbackTask` 的 false 回调表示 Failed。下载的失败通知 `OnFailed()` 在 Failed 终态确定后调用，重试期间仍为 Running。

状态名称从 `InProgress`、`Completed` 分别改为 `Running`、`Succeeded`，枚举数值及行为不变；使用旧名称的调用方需要更新引用。

CoroutineTask 及其基类捕获自有 IEnumerator 的 MoveNext/Current 异常，并释放自有的嵌套迭代器；取消不会调用成功回调。被 yield 的其他 ITask 只被等待，仍由其原调度器管理，不传播它的失败或接管其释放。Unity 原生 yield 操作仍由 Unity 推进，不将通用 AsyncOperation 的结束解释为其业务成功。

迁移自定义任务时，原 Cancel override 改为 `OnTerminated()` 并检查 State；该钩子执行时 IsDone 已为 true，不能用 `if (IsDone) return` 跳过收尾。终态方法及完成回调沿用主线程约定，后台回调应先投递主线程。取消只结束逻辑任务，不保证原生操作已停止；迟到结果仍由具体适配器按原有所有权规则处理。

生命周期入口迁移：原 `public override void Dispose()` 改为 `protected override void OnDispose()`，继承链中的 `base.Dispose()` 改为 `base.OnDispose()`，保持原有清理顺序。外部不再对任务使用 using、手动 Dispose 或 ITick 驱动，也不能直接调用 Execute / Fail；通过 TaskSystem 提交并以 Cancel 请求取消。独立 ITask 实现需迁移为 BaseTask 派生类后才能提交。AssetHandle / SceneHandle 等 Handle 的公开 Dispose 不受影响，仍由业务释放其资源所有权。

## 结果与完成通知

Result 是任务已经提交的最终结果，Task 提供等待并返回该结果，callback 提供主动通知。Bundle / Asset 加载任务不再另存一份最终结果，GetBundle / GetAsset 从 Result 读取，成功时与回调参数指向同一对象。包装器清理不清空 Result；GetAsset 在清理后也保留相同引用，不再单独变成 null。加载 Scene 时用于事件匹配的候选场景及 GetScene 保持原有语义。

完成回调在调用前取出并清空，成功路径仅在 Complete 接受结果后通知，最多发送一次。通知顺序保持原样：成功路径通常先发布 Task 等待结果，再调用 callback；失败回调通常发生在 OnTerminated 内、已有 Task 等待被发布之前。UniTask 续体可能同步重入；await Task 只表示逻辑终态已确定，不表示 callback 中的业务处理也已结束。首次在终态回调内取得 Task 时，它可以立即完成。取消、关闭和迟到原生结果仍沿用各适配器原有规则，不承诺所有终态都调用 callback。实例化失败回调仍返回空数组，Task / Result 仍返回 null；回调异常被记录，不改写任务终态。WaitCallbackTask 的 done(bool) 是结果生产入口，不属于这里的观察回调。

结果引用不代表独立所有权，也不延长 Unity 资源寿命。UnityWebGetRequestTask 仍返回 DownloadHandler，其生命周期受内部 UnityWebRequest 限制；包装器清理会释放请求及默认随请求释放的 handler。需要数据时应在请求清理前同步读取并复制，不能依赖晚些时候 await 或跨帧保留 handler 后继续读取。当前不改变 WebGet 返回类型或移交 handler 所有权。

## 分组与并发

在 Launcher 的 Task 设置中配置：

| 设置 | 默认值 | 作用 |
| --- | --- | --- |
| `ConcurrencyLimit` | 20 | 默认组的并发上限，构造 TaskSystem 时读取并限制为至少 1 |

TaskSystem 自动注册 Default 和 Flow 两个组：

| 组 | ID | 额度 |
| --- | --- | --- |
| Default | `LiteConst.TaskGroupId.Default`（0） | 使用 Launcher 的 Task 设置 |
| Flow | `LiteConst.TaskGroupId.Flow`（-1） | `null`，没有数量准入上限 |

AssetSystem 在 Provider 初始化前另外注册 `LiteConst.TaskGroupId.Asset`（-2），额度来自 `Asset.TaskConcurrencyLimit`，默认 20，运行时至少为 1；版本包加载也使用该组。这三个 ID 为框架保留，业务不需要再次注册。其他组由调用方注册；有额度组要求正整数，`null` 表示无限额组。组 ID 不可重复，提交到未注册的组会报错；当前不提供运行时更改额度、删除组或任务迁组接口。

原 `TaskSystem.DefaultGroupId` / `TaskSystem.FlowGroupId` 已移至 `LiteConst.TaskGroupId`；Flow ID 从 `int.MaxValue` 改为 -1，调用方应使用常量而非硬编码旧值。

```csharp
// 在初始化阶段注册一次；组 ID 由项目统一约定。
const int workGroupId = 1;
LiteRuntime.Task.RegisterGroup(workGroupId, concurrencyLimit: 2);

var task = new WaitCallbackTask(done => done(true));
LiteRuntime.Task.AddTask(task, groupId: workGroupId, priority: TaskPriority.High);
```

组表示共享额度，Asset 加载任务与普通业务的 Default 组相互隔离。Flow 中的任务仍统一登记、推进和收尾，也计入系统运行统计，但不消耗 Default 或 Asset 的额度。Flow 不代表立即执行，也不限制每帧 CPU 耗时。

同组父任务占满额度后等待同组子任务，仍可能无法继续。调度器不会识别协程或回调内部的等待关系；仅编排子任务或等待结果的流程，应在构造任务后显式传入 `groupId: LiteConst.TaskGroupId.Flow`。实际启动下载、计算等工作的协程或回调任务，仍应使用有额度的工作组。直接使用 UniTask 或 Handle 的等待流程本来就不占 TaskSystem 额度，无需为分组再包装成 Task。独立组也不能消除依赖循环。

额度统计的是已准入、尚未收尾的逻辑任务。取消任务后，底层 Unity 操作不一定已经停止；包装已有的 `AsyncOperation` / `IAsyncResult` 也不会延迟底层操作启动。因此组额度不等于底层操作的严格物理并发上限。

`RunningTaskCount` / `PendingTaskCount` 是所有组（包括 Flow）的运行和排队数量之和，只用于观测，不作为总准入上限。直接 `StartCoroutine()` / `StopCoroutine()` 使用 Launcher 承载协程，不走分组队列。

## 优先级与执行时序

`TaskPriority` 依次为 `Low`、`Normal`、`High`、`Urgent`，只决定同组待执行任务的启动顺序；同级按入队顺序执行。优先级保存在调度记录中，提交后不可修改，任何级别都不会绕过组额度。运行任务不抢占，启动顺序不保证完成顺序；持续有高优先级任务时，低优先级任务可能饥饿。

`AddTask` 只登记，不立即调用 `Execute()`。每轮 Tick 先推进已有任务并收尾，再为各组补位；新启动任务从下一轮开始收到 `Tick()`，同步完成会立即归还额度。回调中提交的新任务可能参与本轮补位，但新注册的组从下一轮开始参与调度。每组本轮准入尝试次数以该组开始补位时的待执行数量为限，避免同步任务无限派生；这不是每帧耗时预算，也不承诺跨组启动顺序。

Asset 模块的 `AssetLoadPriority` 用于 Bundle Loader 自己的稳定优先队列，与这里的 `TaskPriority` 独立。版本包、本地和远程 Bundle 任务在创建入口以 `Urgent` 提交到 Asset 组；Bundle 就绪后的 Asset/Scene 任务以 `Normal` 提交到同一组。它们共享 Asset 额度，不占用 Default，也不享有绕限权限。Bundle Loader 仍保留自己的队列和 `Asset.ConcurrencyLimit` 并发限制；该限制与 `Asset.TaskConcurrencyLimit` 各自生效。Asset 请求提权不会修改已提交任务的固定优先级，也不保证整条加载链优先。场景卸载观察继续进入 Flow，通用下载和异步实例化便利入口继续进入 Default。Editor Provider 的直接加载路径不通过 Asset 组排队。详见 [资源加载优先级](../../../Asset/README.md#加载优先级)。

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
