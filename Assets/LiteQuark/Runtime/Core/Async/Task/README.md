# TaskSystem 任务调度

`LiteRuntime.Task` 调度协程、Unity 异步操作和回调任务，并提供主线程投递。只有加入 TaskSystem 的任务受其调度；可与直接使用 UniTask 的异步方法并存。各任务组拥有独立并发额度，没有跨组总并发上限。

## 提交、等待与取消

示例使用 `LiteQuark.Runtime`、`Cysharp.Threading.Tasks` 命名空间，在框架初始化后的异步方法中调用：

```csharp
var task = new WaitCallbackTask(done =>
{
    // 实际操作完成时调用 done(true)，失败时调用 done(false)。
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

任务是一次性对象，只提交未执行的 Pending 任务，不重复或跨组提交。提交后由 TaskSystem 清理包装器，业务通过 `Cancel()` 请求取消，不手动 Dispose 或驱动 Execute / Tick。资源加载使用 AssetSystem 的 Handle API，Handle 仍由业务 Dispose。

| 入口 | 默认组 | 用途 |
| --- | --- | --- |
| `AddTask(AsyncOperation, callback)`、`AddTask(IAsyncResult, callback)` | Flow | 观察已经启动的操作 |
| `AddMainThreadTask(func, param)` | Flow | 排队执行主线程委托 |
| `AddTask(IEnumerator, callback)` | Default | 启动协程 |
| `AddTask(Action<Action<bool>>)` | Default | 包装回调式操作，done(false) 表示失败 |
| `AddInstantiateGoTask(...)`、`AddUnityWebGetRequestTask(...)` | Default | 异步实例化／下载 |

上述便利入口使用 Normal 优先级。指定组或优先级时先构造 BaseTask 派生对象，再调用 `AddTask(ITask task, int groupId = LiteConst.TaskGroupId.Default, TaskPriority priority = TaskPriority.Normal)`。独立实现 ITask 的对象会被拒绝；通用入口只按传入组登记，不按类型自动分组。

## 状态、结果与通知

| 状态 | 结果和等待语义 |
| --- | --- |
| Pending / Running | 等待准入／正在执行 |
| Succeeded | 最终结果允许为 null，Progress 为 1 |
| Failed | Error 为非空异常，Result 为 null |
| Cancelled | Error 和 Result 均为 null |

`IsDone` 包括三个终态；终态不可逆。`Task` 为 `UniTask<object>`，**失败和取消都以 null 完成，不因这两种终态抛异常**，因此要检查 State / Error。终态前后取得 Task 的结果语义一致；失败或取消保留最后进度，不能用 Progress 判断成功。

取消结束逻辑任务，不保证 Unity 原生操作停止。观察已有 AsyncOperation / IAsyncResult 不会延迟底层操作的启动，完成也不等于其业务结果成功。

四类内置资源加载任务另实现内部 `IResourceTaskCleanup.WaitForCleanupAsync()`，可在完成前后重复等待。它只观察任务自身的原生收尾，不执行、取消或释放任务，也不加入新的调度任务；未启动的 Pending 任务仍须执行或终结后才能结束收尾。

- LocalBundle 取消或失败后卸载迟到 Bundle；Asset 等待原生读取结束，不自行销毁所属 Bundle 中的对象；Scene 取消或失败后卸载迟到场景并等待实际卸载。
- 下载基类在终态立即停止重试计时器并释放请求，不依赖调度器回收包装器；结果通知期间的释放推迟到完成回调的 finally。重试只释放本轮请求，保留下一轮计时器。
- RemoteBundle 在基类报告终态资源释放后结束收尾等待，因此成功通知先于 Cleanup 完成。它不为清理而提取 Bundle，也不承诺底层线程已停止。请求释放失败由收尾等待报告；若仍在准备重试，则停止重试并使任务失败，已经确定的逻辑终态保持不变。
- 正常成功结果继续按原路径交接，包装器回收不丢失成功通知或卸载结果。原生加载失败但无遗留资源时，收尾可以成功；迟到清理失败只使收尾等待抛异常，不改写逻辑终态。
- 该内部能力尚未接入 AssetSystem 关闭；Asset 原生读取期间，上层仍必须保留所属 Bundle，不能依据 IsDone 提前卸载。

- Result 是最终结果；包装器清理不会清空它，但引用不代表独立的 Unity 资源所有权。
- 完成通知至多一次，不承诺取消或关闭时发送 callback。回调异常被记录，不改写终态。
- await Task 只表示逻辑终态已确定，不表示 callback 中的业务处理完成。成功路径通常先发布等待结果再调用 callback，失败通知通常在已有等待恢复之前；续体可能同步重入，不能假定所有任务的通知顺序相同。
- 协程捕获并释放自有嵌套迭代器，取消不调用成功回调；yield 其他 ITask 只等待，不传播其失败或接管其释放。
- 异步实例化失败时，callback 收到空数组，Task / Result 为 null。

`AddUnityWebGetRequestTask` 的成功结果为 `byte[]` 快照，callback、Result 和 await 返回同一数组，原生请求清理后仍可读取。按只读数据使用，需要修改时自行复制，文本由业务按协议解码。空响应成功时为空数组；失败结果为 null、失败回调传 null；取消不发送完成回调。快照由托管引用维持，业务无需 Dispose。

## 分组与并发

| 保留组常量（LiteConst.TaskGroupId） | ID | 额度 |
| --- | --- | --- |
| Default | 0 | Launcher 的 Task.ConcurrencyLimit，默认 20，构造时限制为至少 1 |
| Flow | -1 | null，不限制准入数量 |
| Asset | -2 | AssetSystem 注册；配置与加载行为见 [Asset 指南](../../../Asset/README.md#加载优先级) |

业务使用其他 ID 注册组，有额度组要求正整数，null 表示无限额。ID 不可重复，提交到未注册组会报错；不提供运行时改额度、删除组或迁组接口。

```csharp
const int workGroupId = 1; // 项目统一约定，初始化阶段注册一次。
LiteRuntime.Task.RegisterGroup(workGroupId, concurrencyLimit: 2);
var task = new WaitCallbackTask(done => done(true));
LiteRuntime.Task.AddTask(task, groupId: workGroupId, priority: TaskPriority.High);
```

同组父任务占满额度后再等待同组子任务，可能无法继续。仅编排或等待子任务的流程可显式放入 Flow；实际启动下载、计算等工作的任务使用有额度组。直接 UniTask / Handle 等待不占 TaskSystem 额度，无需额外包装。独立组不能消除依赖循环。

Flow 不代表立即执行，也不限制每帧 CPU 耗时。额度统计已准入、尚未收尾的逻辑任务，不是底层原生操作的严格并发上限。`RunningTaskCount` / `PendingTaskCount` 包含所有组，仅用于观测。`StartCoroutine()` / `StopCoroutine()` 直接使用 Launcher，不经过组队列。

## 优先级与执行时序

`TaskPriority` 为 Low、Normal、High、Urgent，只决定同组待执行任务的启动顺序，同级 FIFO。提交后不可更改优先级，不抢占运行任务，也不绕过额度；启动顺序不保证完成顺序，持续高优先级任务可能使低优先级任务饥饿。

AddTask 只登记，Execute 在后续调度中调用。每次 Tick 先推进已有任务并收尾，再补位；新启动任务从下一轮收到 Tick，同步完成会释放额度。回调提交的任务可能参与当前轮补位，新注册组下一轮参与；不承诺跨组启动顺序或每帧耗时预算。

## 自定义任务

继承 [BaseTask](Base/BaseTask.cs)，构造阶段只保存参数，需释放的资源在 OnExecute 中取得。通过受保护的 Complete / Fail 发布结果，不自行改写终态。

| 扩展点 | 责任 |
| --- | --- |
| `OnExecute()` | 启动工作；成功调用 Complete(result)，失败调用 Fail(exception) |
| `OnTick(deltaTime)` | 可选的逐帧推进 |
| `OnTerminated()` | 终态已确定时停止操作或发送终态通知；此时 IsDone 已为 true，不能因此跳过清理 |
| `OnDispose()` | 调度器移除任务时释放包装器资源，不改写已发布的 Result / Error |

Complete 仅在 Running 时接受结果并返回 true；重复终态调用不重复通知，Fail(null) 抛参数异常。OnTerminated 在结束已有等待前执行，不用于补算结果。清理至多尝试一次，重入或清理异常不触发重试；通知和清理异常被记录，不覆盖原始失败。

Execute / Tick 的异常由调度器转为 Failed。自定义的异步或原生回调需自行捕获执行异常并调用 Fail，不用 SafeInvoke 吞掉执行失败。所有终态方法在主线程调用，后台回调先投递主线程；取消后的迟到结果仍须由任务适配器按其所有权规则处理。ITask 是观察和取消接口，internal 生命周期方法不是业务扩展点。

## 主线程与关闭

```csharp
LiteRuntime.Task.PostMainThreadTask(_ => LLog.Info("Main thread"), null);
```

- `PostMainThreadTask(callback, state)` 投递到初始化时捕获的同步上下文。
- `SendMainThreadTask(callback, state)` 同步发送，可能阻塞调用线程。
- `AddMainThreadTask(func, param)` 加入 Flow，由后续调度执行。

不要从后台线程访问 Unity 对象或修改任务队列，也不要让主线程等待正在等待主线程的工作。不递归调用 Tick，不在任务回调中释放 TaskSystem。关闭须先停止调度，清理回调不再提交任务；系统会停止 Launcher 上的协程并取消、释放登记任务。关闭开始后，所有 AddTask 提交入口抛出 ObjectDisposedException；被拒绝的任务仍由调用者负责，系统不接管或改变它的状态。

完整接口见 [TaskSystem.cs](TaskSystem.cs)，升级步骤见 [CHANGELOG](../../../../CHANGELOG.md)。
