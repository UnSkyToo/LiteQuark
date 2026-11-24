# Changelog

## **[Unreleased]**

### 从 Launcher 的 DebugSetting / ReleaseSetting 手工迁移

1. **在仍使用旧框架的工程中先备份，再升级。** 提交或复制含 Launcher 的启动场景、Prefab 及其 `.meta`。使用 Prefab 实例的场景也要保留其覆盖值。分别记录 `ReleaseSetting` 和 `DebugSetting` 的 GameEntry、SystemList 顺序与 Disabled 标记、各模块数值、SystemSettings 及其中的场景对象引用（尤其是 UI 相机）。新版读取不到旧字段，直接重新保存会丢掉旧配置。如果已经升级，从版本控制或备份中用旧版查看原值。
2. 升级后在 Launcher 选 **Release**，用旧 `ReleaseSetting` 的 Common、Task、Asset、Action、Log 填正式默认值。把旧 `Asset.BundleMode`（曾名 `BundleLocater`）和 `BundleRemoteUri` 填入 Asset。日志按**旧构建实际生效的级别**迁移。旧正式包使用 Error/Fatal，控制台关闭。旧 Development 在 `Common.DebugMode` 和 `Log.ReceiveLog` 都生效时使用配置的日志级别和控制台开关，否则也使用 Error/Fatal 并关闭控制台。把这两套结果分别填入 Release 和所需的 Development 覆盖。新版由环境和显式日志策略决定这些行为。
3. 同在 Release 页恢复 **Game Entry**，用 **System List** 的 **+** 依原顺序添加系统并恢复启用状态。将旧 `SystemSettings` 中每种配置的值填入对应注册项，重新拖入原来的 `UISystemSetting.UICamera`。两个旧页签若使用不同入口、系统列表或普通字段，项目需明确选定这一份共用值。
4. 切到 **Development**，对照旧 `DebugSetting`。需要差异时，给资源模式、CDN、日志级别、控制台或 `ThrowException` 勾选 **Override** 并填开发值，其余使用 Release。旧 `_simulateReleaseBuild` 的编辑器预览用途改为 Launcher 的 **Environment**。Development Player 和普通 Player 分别按自身构建类型取值。
5. 核对后保存并重新打开场景／Prefab，分别检查两种环境下的入口、系统顺序、UI 相机、资源来源和日志策略，再运行实际启动流程。此前使用过实验版 `LiteSettingsAsset` / Prefab Variant 的项目，可按相同步骤从各环境生效值转填，验证后再移除旧资产。

### 核心接口

- 移除 `LiteConst.InternalSystem`。原先通过该字典添加的自定义系统请移入 Launcher 的 **System List**，按依赖先后排列。内置系统顺序由框架固定；原先依赖插入两个内置系统之间的初始化逻辑需调整到内置系统全部就绪后执行。
- 破坏：Launcher 使用一份场景配置和显式 Development 覆盖。外部旧场景和 Prefab 需要按上节手工迁移。
- 扩展系统和多态参数合并为一个 `LiteSystemEntry`；把旧 Addon 参数移入对应注册项。以前用 `SystemSettings` 或按设置类型匹配的代码应改为给注册项的 `Setting` 赋值。程序化初始化可继续使用有效的 `LiteSetting`；通过 `LiteRuntime.Setting.Environment` 查询环境。
- 新增：游戏入口和系统可独立实现 `ILateTick`。Runtime 在 LateUpdate 先调用入口，再按注册顺序调用系统，复用本帧 Tick 的 deltaTime。

- 破坏：移除 `ILogic`、`LogicList` 和 `LogicCenter`。将业务启动和释放集中到一个 `IGameEntry`，需要逐帧更新时显式实现 `ITick`。在 Launcher 上重新选择 **Game Entry**，留空只启动系统。
- 日志控制台由 LogSystem 创建和销毁，移除 `LogRuntimeLogic` 配置。迁移后核对两个环境的 `Log.ShowLogViewer`，新配置默认关闭。

- 修复：Editor 停止 Play Mode 后的异步清理使用 Unity 对象判空，日志逻辑跳过已销毁的日志窗口，TaskSystem 跳过已销毁的协程宿主，避免编辑模式 Destroy 错误和 MissingReferenceException。
- 破坏：Restart 仅在 Running 接受请求并在下一帧执行，多次请求合并；ShuttingDown 期间忽略请求，其他状态抛出 InvalidOperationException。启动错误回调中的直接 Restart 需改为等待清理回到 Idle 后再 Startup；已排队请求遇到错误或关闭会被放弃。
- 破坏：单个入口或系统释放异常改为记录日志并继续释放其余系统。关闭结束回到 Idle。本次自动重启遇错停止，修正错误后可显式 Startup。
- 修复：启动错误订阅者抛异常时继续回滚。重启关闭期间收到应用退出通知时取消下一轮启动。
- 修复：后台超时返回前台时，仅在前台事件处理后仍为 Running 才请求自动重启，避免初始化或错误状态触发 Restart 异常。
- 破坏：`ISystem`、`ILogic` 改为继承 `IDisposeAsync`，释放方法由 `void Dispose()` 改为 `UniTask DisposeAsync()`；自定义实现需要迁移，同步清理返回 `UniTask.CompletedTask`。已注册对象由 Runtime 统一释放，业务不要直接释放系统；Handle、Lease 等资源对象仍使用同步 Dispose。
- 破坏：`LiteRuntime.Shutdown()` 返回 UniTask。集成层应等待其完成，初始化中的对象在 Initialize 退出后释放。
- 破坏：`ActionSystem.GetActionList()` 收为 internal，仅用于框架调试；外部业务通过 AddAction、StopAction、FindAction 等公开入口管理动作，不直接访问调度列表。
- 调整：Runtime.Shutdown 不再统一调用 PlayerPrefs.Save、Resources.UnloadUnusedAssets 或 GC；依赖原退出保存行为的业务应显式保存，DataSystem 的 PlayerPrefsProvider 保留自身保存行为。
- 修复：Center 先就地释放未发布实例，再由 Runtime 回滚已发布对象；系统开始释放时退出更新列表，依赖在关闭期间继续推进。
- 调整：Timer 关闭时取消已有定时器，Action 关闭时 Stop 后 Dispose，均不额外触发完成回调；Log 释放时重置 LLog 的静态 logger 缓存。异步释放接口不代表各系统已经实现完整原生资源收尾。
- 破坏：移除 `ISubstance`；实现类改为直接实现 `ITick` 和 `IDispose`，原先使用 `ISubstance` 的调用方应按所需能力使用对应接口。
- 破坏：移除 `IStartup` 和 `IShutdown`；其他使用方需改用自己的生命周期接口。

### ObjectPool

- 修复：移除池时，取消回调中创建的同 key 新池不会被外层移除误删；旧实例不能移除新池的登记。
- 修复：系统关闭时的取消回调移除池不会中断批量清理；自动回收在实际移除前重新检查借出数量，保留被前序回调借用的池。
- 破坏：`IBasePool.WaitReadyAsync` 和 `PreloadPoolAsync` 改为 `UniTask<bool>`，移除 `Generate(count, callback)`，预生成改用 `await GenerateAsync(count)`；普通失败检查 false，关闭取消按 `OperationCanceledException` 处理。自定义 IBasePool 实现需更新接口。
- 调整：Alloc 回调签名保留，失败或取消均通知 null。调用方检查 null，回收时保存并使用原池。
- 迁移：BaseGameObjectPool 的分配回调现在包装异步 `Alloc(Transform)`；自定义池需要同时影响两种入口时，覆写异步重载。模板等待改为保存终态，移除 `LoadTemplateCallbackList`／`RunWhenLoadTemplated`；派生池通过 `OnLoadTemplate` 报告结果，池关闭时统一取消等待。
- 迁移：预生成实例直接入池并触发 `OnRelease`，不再经过公开的 `Recycle`；自定义池若将预生成也需要的复用初始化写在 `Recycle` override 中，应移到 `OnRelease`。
- 修复：池同步关闭结束模板等待与预生成，先停止实例化再释放模板；初始化失败的池也会清理。预生成后的活动数量正确参与自动回收判断。
- 修复：实例化 Task 清理已完成但未发布的对象，成功发布后的结果不会被取消或包装器清理误销毁。

### Asset

- 调整：ResCollector 和 ResCollectorSetting 移回 LiteQuark.Editor 程序集，保留命名空间及脚本 GUID；Simulate 的 Provider、Task 和 Resource 继续位于 Runtime，通过 Editor 注册入口取得清单与独立源路径快照。
- 修复：默认 Collector 在实际收集时取得配置快照，保留 PreProjectBuild／PreStepBuild 修改分包规则的行为；清理清单缓存后重新读取配置。模拟版本任务只通过任务结果交付清单，移除未使用的 callback 通道。
- 行为变更：缺少收集配置时使用默认分包规则，Simulate、构建收集、Bundle Viewer 和文件夹 Inspector 的只读绘制不再自动创建 ResSetting.asset。依赖自动生成配置的工具需改为显式创建；现有配置无需重建，文件夹分包选项实际修改时仍会创建并保存配置。

- 调整：本地、远端和模拟 Bundle Task 统一交付 IBundleResource；Provider 的清单和 Bundle 加载接口移除结果 callback，清单通过返回任务读取，Bundle 由 Loader.Tick 统一接收终态。依赖任务成功后到 Cache 收到结果之间可能多一次 Loader Tick；已交付缓存的命中行为不变。
- 破坏：ILoadBundleTask 改为内部接口，GetBundle 返回 IBundleResource；任务结果观察不取得额外所有权。外部资源消费者继续使用 AssetSystem Handle API，不依赖内部 Bundle 任务。
- 修复：Loader 关闭释放已成功但尚未交付的容器，已交付容器仍归 Cache，取消／失败的迟到原生结果仍归 Task。

- 修复：后端创建移至 AssetSystem 后，非法模式和无效 Remote URI 继续通过初始化 UniTask 交付原始异常，不从 Initialize 调用同步抛出；任务组注册的同步异常语义不变。

- 破坏：配置字段 BundleLocater 改名为 BundleMode，类型 BundleLocaterMode 改为 BundleLoadMode；代码调用方更新名称，枚举数值仍为 Simulate=0、BuiltIn=1、Remote=2。旧双配置布局曾通过 FormerlySerializedAs("BundleLocater") 读取原值；迁入本版 Launcher 时需按上节手工转填。JSON 和其他自定义解析器也需更新字段名。
- AssetSystem 直接持有 AssetManager（原 AssetBundleProvider）。IBundleLocater 及实现改名为 IBundleProvider、SimulateBundleProvider、BuiltInBundleProvider、RemoteBundleProvider，并沿用原有 GUID。
- 调整：后端选择和非 Editor 的 Simulate→BuiltIn 回退集中在 AssetSystem.Initialize；Manager 初始化接收已选好的 IBundleProvider，不再自行判断模式或平台。缓存、依赖、Lease、Retention 与任务调度行为不变。

- 破坏：删除 AssetEditorProvider、AssetProviderMode、AssetSetting.AssetMode，以及 SimulateAsyncDelayInEditor／AsyncDelayMinFrame／AsyncDelayMaxFrame；只通过 BundleLocater 选择模式。代码配置改为 BundleLocaterMode.Simulate、BuiltIn 或 Remote。
- 迁移：BundleLocater 数值调整为 Simulate=0、BuiltIn=1、Remote=2。旧 AssetMode=Editor 的编辑器开发配置迁到 Simulate；旧 AssetMode=Bundle 的配置按语义将旧 BuiltIn=0→1、Remote=1→2、Simulate=2→0。外部场景、Prefab、设置资产和自定义数字配置须在更新后明确迁移，不能只删除 AssetMode 后沿用旧 Locater 数字。仓库内 Demo 与 LiteNexusEditorScene 已迁移并保留资产 GUID。
- 调整：默认 Simulate 在 Editor 中执行清单收集及共享 Bundle 管理链路，在非 Editor（含 Development Build）自动使用 BuiltIn，仍需构建并部署本地资源包；原 Editor 配置若在 Player 使用 Remote，应明确改为 Remote。不会自动改写保存的 Locater 值。
- 调整：移除无 Cache 的资源 Lease 和预先加载场景的特殊入口。模拟资源必须收录于初始化清单，缓存命中可能同步完成；删除旧直读模式的随机延迟行为，修改收录／分包／依赖后需重新初始化。

- 破坏：调用 `LoadSceneHandle(path, priority, ct)` 时移除原 `LoadSceneParameters` 参数。业务场景固定 Additive 加载，使用默认物理场景；原先通过 Single 切关的项目改为保存并释放旧关卡的 SceneHandle，保留启动场景和 Launcher。
- 破坏：同一轮 Runtime 内，同一路径从开始加载到实际卸载只允许一个业务场景。重复请求请复用已持有的 Handle。跨轮重开同一路径时，业务须等旧场景实际卸载后再请求。需要并存的场景使用不同路径。
- 破坏：同进程 Restart 和显式 Shutdown 在关卡仍在场时也会执行。需要清空关卡时，由业务释放已交付的 SceneHandle 并确认场景退场。旧 Bundle 随资源系统关闭释放。
- 调整：业务退场时停止发起加载。AssetSystem 开始释放后拒绝新请求，等待原生读取，再忽略 Handle 引用和 Retention 卸包。
- 迁移：Provider 实现改为 IDisposeAsync，关闭调用方须等待终结完成；Handle／Lease 仍同步 Dispose。业务须停止使用旧资源，遗漏 Handle 不延长系统生命周期。
- 修复：关闭期间的迟到资源得到清理，已交付结果不会被加载任务误释放；正常运行时场景实际卸载后归还 Bundle 引用。
- 破坏：公共加载统一为 Handle API，移除回调加载、非 Handle 加载、按路径／对象释放和手动场景卸载入口。实例化使用 `InstantiateHandle`，批量预加载使用 `PreloadAssetHandle<T>`，使用结束后 Dispose。
- 破坏：四种 Handle 的 Task 属性改为 internal；`var result = await handle.Task` 改为 `await handle; var result = handle.Result;`。Handle 必须覆盖资源使用期，不能同步读取未完成的结果。
- 破坏：`IAssetHandle<T>` 增加 State、Error、IsDisposed，自定义实现需补齐。取消或等待中 Dispose 立即结束请求；终态后 Dispose 保留 State / Error。
- 破坏：AssetError 删除 Code、Stage、Cause 及对应枚举；使用 `AssetError(string resourceKey, string message, Exception exception = null)`。ResourceKey 指向实际失败资源或依赖 Bundle，调用者不能假定它等于请求路径或继续遍历 Cause。
- 新增：PreloadHandle.Errors 与 FailedPaths 按索引对应；部分失败时仍持有成功项直到 Dispose。
- 预加载与其他资源 Handle 统一在 Unity 主线程取消。使用 `CancellationTokenSource.CancelAfter` 的预加载调用方，改在主线程计时回调中调用 `Cancel()`。
- 新增：加载入口支持 AssetLoadPriority；旧的 `(path, token)` 调用改为 `(path, ct: token)`，或显式传 priority 后再传 ct。Asset 任务额度使用独立的 `Asset.TaskConcurrencyLimit`，与 Loader 的 ConcurrencyLimit 分别生效。
- 破坏：移除 AssetRetainTime，仅配置 BundleRetainTime；资源无引用后不再额外等待 Asset 保留期。
- 破坏：移除 Resource 模式，运行时使用 Bundle；随包资源使用 `BundleLocaterMode.BuiltIn`。
- 优化：SceneHandle 按实际实例卸载，外部卸载后 Result 为空；战斗配置在数据管理器启动时异步加载，初始化失败或关闭时释放。
- 修复：资源类型不匹配及失败路径的引用泄漏；VersionPack 解析错误状态。
- Bundle 依赖环统一在 Editor 收集清单时报告。自行生成 BuiltIn 或 Remote 版本文件的项目，应将依赖环检查放在导出或发布阶段。原先依赖启动时 `FrameworkErrorCode.LoadVersionPack` 报告环路的集成，应在发布前处理校验失败。

### Task

- 破坏：ITask 不再继承 IEnumerator，BaseTask 移除迭代器实现。`yield return task` 改为 `yield return task.Task.ToCoroutine()`，异步方法使用 `await task.Task`；两者都只等待已提交任务。旧 yield 写法仍可能编译通过，需主动迁移，不能依赖编译检查发现遗漏。
- 破坏：通用提交入口由 `AddTask(ITask, ...)` 改为 `AddTask(BaseTask, ...)`。提交任务的参数／工厂返回值使用 BaseTask 或具体派生类型；提交后用于观察、等待和取消的引用仍使用 ITask。独立实现 ITask 不提供调度资格。
- TaskSystem 在释放期间停止接收任务。依赖 `ObjectDisposedException` 判断能否提交的调用方，应在自身释放时停止提交，重启后重新取得调度器。
- 调整：下载任务终结后停止重试并释放请求；回调完成后释放本轮请求，传输失败仍按配置重试。
- 破坏：任务取消统一使用 `BaseTask.Cancel()`。原先覆写 `Cancel()` 的实现可在 `OnTerminated()` 中处理 Cancelled 状态。自定义下载任务移除 `OnResourcesReleased` 覆写。派生自 `UnityDownloadBaseTask` 的任务覆写 `OnTerminated()` 时必须调用 `base.OnTerminated()` 以停止重试并释放请求。自定义收尾可能抛错时，将基类调用放在 `finally` 中。
- 修复：await 续体中回收任务包装器不会丢失成功结果通知，也不会中断迟到场景清理。
- 破坏：TaskState.InProgress / Completed 改名为 Running / Succeeded，更新枚举引用；失败和取消等待仍返回 null，调用者检查 State / Error。
- 破坏：提交对象必须派生 BaseTask。ITask 不再继承 ITick / IDispose，外部通过 TaskSystem 提交、Cancel 取消，不直接 Execute / Tick / Dispose / Fail。
- 破坏：自定义任务的 Cancel override 改为 OnTerminated 并检查 State；钩子执行时 IsDone 已为 true。Dispose override 改为 OnDispose，继承链的 base.Dispose 改为 base.OnDispose，保留清理顺序。
- 破坏：WebGet 构造函数及 AddUnityWebGetRequestTask 的回调改为 `Action<byte[]>`；成功 Result / await 的实际类型也为 byte[]。将 handler.data 改为数组，handler.text 改为业务解码，移除 DownloadHandler 强转。
- 破坏：DefaultGroupId / FlowGroupId 移至 LiteConst.TaskGroupId；Flow ID 从 int.MaxValue 改为 -1，使用常量而非旧硬编码。
- 新增：任务组独立额度及稳定优先级队列；观察已有 AsyncOperation / IAsyncResult 和主线程委托便利入口使用 Flow。自定义流程须显式选组，避免同组父子等待耗尽额度。

### 路径与构建

- 编辑器保存资源根下 `Assets/...` 子目录中的资源时，使用完整项目路径。若旧字段已误存为 `Assets/UI/Hud.prefab` 一类值，请重新选择对应资源，或从源资源重新生成数据库。
- 资源路径按清单键匹配。将配置中的 `./` 和重复分隔符改为实际资源路径。此前因控制字符、特殊文件名字符、绝对路径或 URL 被同步拒绝的带扩展名输入，现在通常得到缺失资源的 Failed Handle。批量预加载会继续处理同批有效资源。
- `AssetPath` 对外只保留单路径 `ToProjectPath` 和 `ToRootRelativePath`。原先调用 `Resolve`、`TryResolve`、`ResolveScene` 或 `ResolveProjectAssetPath` 的业务改用 `AssetSystem` 查询和加载。原先使用 `NormalizeProjectPath` 统一分隔符的工具可用 `PathUtils.UnifyPath`。
- 迁移 `TryGetRelativeToRoot` 时，保存供编辑器回读的资源字段用 `AssetPath.ToRootRelativePath`。判断路径是否位于指定目录下或生成相对名称时，先统一分隔符，再用 `StringComparison.OrdinalIgnoreCase` 匹配目录前缀 `root.TrimEnd('/') + "/"`。匹配且后缀非空时，截取后缀即可得到保留原始大小写的相对名称。
- 修复：JSON 和二进制版本清单导出只在输出中编码依赖 ID。同一清单可重复导出，成功和失败均保留内存中的依赖路径。
- 破坏：版本清单保存完整规范资源路径。旧相对路径清单须从源资源重新构建并与新读取器配套；读入旧清单再导出不能可靠迁移，解析成功也不证明兼容。
- 破坏：ConcatPath 按宿主 Path.Combine 处理后续根路径，不裁剪其前导分隔符；URI 组合改用 ConcatUri，传未编码路径片段。
- 破坏：下列 PathUtils API 移除，调用方更新入口：

| 旧入口 | 替换方式 |
| --- | --- |
| GetFullPathInAssetRoot | AssetPath.ToProjectPath |
| GetRelativeAssetRootPath（含数组） | AssetPath.ToRootRelativePath，数组逐项转换 |
| FormatAssetPath / AssetPath.NormalizeBundlePath | 从清单读取 BundleInfo.BundlePath |
| PathIsFile、GetPathFromFullPath、FileExists、DirectoryExists、CreateDirectory | 按实际需求使用 System.IO.File / Directory / Path；资产目录判断用 AssetDatabase.IsValidFolder |
| GetLiteQuarkRootPath | 编辑器使用 LiteEditorUtils.GetLiteQuarkRootPath |
| GetFullPathInRuntime | 资源定位交由 BundleBuiltInLocater；普通持久化目录使用 GetPersistentDataPath |
| ToFlatPath | Bundle 物理路径使用 VersionPackInfo.GetBundleFileBuildPath / GetBundleFileLoadPath |

- 修复：带点号目录的错误分包，受影响资源需要重新构建；FlatMode 物理文件名碰撞会在构建映射阶段报错，须调整目录／命名或关闭 FlatMode。
- 文档：接入与模块指南说明当前能力和使用契约；升级步骤集中于此。

## **[0.4.3] - 2026-03-04**
- 修复：Uri大小写导致的版本文件无法获取

## **[0.4.2] - 2026-02-28**
- 新增：Action 增加 UnscaledTime 支持
- 新增：Debugger 面板补全 Timer 和 Event 调试信息，Task 显示并发占用情况
- 新增：ResCollectorSetting 增加清理无效目录的功能
- 优化：统一 AssetPath 的处理
- 优化：报错后不再立即暂停框架，改为连续异常熔断机制；EnterErrorState 支持传入 FrameworkErrorCode
- 修复：Release 下所有日志都关闭的 BUG，保留 Error 和 Fatal 级别

## **[0.4.1] - 2026-02-26**
- 新增：Timer 支持 Unscaled 模式，不受 TimeScale 影响
- 新增：TimerSystem 增加 CancelAll 接口
- 新增：LiteUtils.SafeInvoke 增加多参数重载，统一全框架回调异常保护
- 优化：重构 Timer 模块，支持单帧多次触发（MaxTicksPerFrame 上限），修复延迟溢出时间丢失
- 优化：Timer 的 totalTime 计算改用 Round 替代截断，提高精度
- 优化：提取 BaseCoroutineTask 基类，消除 CoroutineTask/AsyncOperationTask/AsyncResultTask 重复代码
- 优化：SafeList/SafeDictionary 迭代结束立即 Flush 延迟操作；Clear 先清空队列再入队
- 优化：SafeList 的 GetEnumerator 增加迭代保护；Sort 在迭代中抛出异常
- 优化：启动流程贯穿 CancellationToken，Shutdown 可安全取消进行中的初始化
- 优化：Shutdown 增加 Idle 状态守卫，防止重复关闭
- 优化：移除多余的 CallbackFuncAction 和 4 参数 Callback/DelayCallback 变体
- 优化：Utils 模块重组，通用方法从 UIUtils 迁移至 UnityUtils；删除 JsonUtils 死代码
- 优化：全框架 callback?.Invoke 统一替换为 SafeInvoke
- 优化：内部类统一 sealed 声明；static lambda 消除闭包分配
- 优化：TaskSystem 接口命名统一（Add 前缀）；Fsm 命名优化（CanChangeTo）
- 优化：统一 IsEnd → IsDone、MarkSafety → MarkAsSafe 等命名
- 修复：System 未受 Setting.TimeScale 影响的 BUG
- 修复：LogAppender 析构函数条件写反，导致未关闭的 Appender 跳过清理
- 修复：场景已加载时 EditorProvider 返回 false，与其他 Provider 结果不一致的 BUG
- 修复：TypeUtils.IsListType/IsDictionaryType 仅按泛型参数数量判断导致误判的 BUG
- 修复：ReflectionUtils.GetPropertyInfo 两个重载默认 BindingFlags 互换的 BUG
- 修复：PositionGameObjectPool 继承 BaseGameObjectPool 导致无模板加载的 BUG
- 修复：LogErrorHandler 方法体为空，日志系统自身错误被静默吞掉
- 修复：LoggingEvent Info 级别不必要的 StackTrace 开销
- 修复：Task 移除 Abort 概念，统一为 Cancel

## **[0.4.0] - 2026-02-25**
- 新增：AudioSystem 增加暂停和恢复接口
- 新增：ActionSystem增加DelayCallback
- 新增：增加 RetryParam，提供重试间隔的参数
- 新增：AssetSystem增加HasAsset接口
- 新增：AssetSystem UniTask Load接口提供Cancel机制
- 新增：AssetSystem 增加Handle模式接口
- 优化：简化 AssetCacheStage 状态，移除 Unloading 瞬态
- 优化：废弃 ListEx，统一使用 SafeList
- 优化：无网络情况下的请求直接失败，避免等待超时
- 优化：更安全的框架启动流程，捕获异步异常
- 优化：InstantiateAsync 不再重复注册 prefab 到 IDToPathMap
- 修复：Bundle 加载失败后状态未重置，导致无法再次加载的 BUG
- 修复：多播委托异常导致后续 Bundle 回调丢失的 BUG
- 修复：回调中重新加载资源导致的迭代器修改异常
- 修复：加载失败时 Bundle 引用计数异常增加的 BUG
- 修复：依赖 Bundle 加载成功但主 Bundle 失败时，引用悬空的 BUG
- 修复：依赖信息缺失时 Bundle 加载卡死的 BUG
- 修复：未加载的 Bundle 执行 Unload 破坏依赖引用计数的 BUG
- 修复：Asset 加载失败后 AssetInfoCache 不过期导致的内存泄漏
- 修复：依赖加载失败时主 Bundle 的 AssetBundle 对象未释放的内存泄漏
- 修复：DecRef 在非法状态下仍执行导致引用计数下溢的 BUG
- 修复：Dispose 时未触发 pending 回调导致 UniTask 无法 resolve 的 BUG
- 修复：场景未加载时卸载导致引用计数错误的 BUG
- 修复：事件响应中抛出异常导致后续事件中断的 BUG
- 修复：事件回调中反注册导致集合修改异常的 BUG
- 修复：DownloadTask 重试未释放 Request 的 BUG
- 修复：取消任务时未取消下载重试计时器的 BUG
- 修复：ObjectPool 模板加载回调的多播问题
- 修复：ParallelAction/SequenceAction 中错误的提前 Dispose 行为
- 修复：SafeList 和 SafeDictionary 在 Foreach 中 Clear 导致的迭代器异常
- 修复：Attribute 顺序错误导致 Condition 属性未生效的 BUG
- 修复：TaskSystem 异常路径下 RunningTaskCount 不一致的 BUG

## **[0.3.0] - 2026-02-09**
- 新增：NetworkSystem 网络模块
- 新增：DataSystem 数据持久化模块
- 新增：Load Profiler模块，资源加载分析器
- 新增：ObjectPool增加UniTask接口版本
- 优化：Setting配置方式，支持自定义System配置Setting
- 优化：重构EntryData，System和Logic共享，也支持自定义的数据展示
- 优化：打包脚本部分设置前置，放置没有勾选App阶段跳过了必要设置
- 优化：Task增加更详细的任务名称和打印
- 修复：编辑器模式重启切后台报错
- 修复：资源加载中重启框架报错
- 修复：Bundle Viewer大小计算异常

## **[0.2.6] - 2025-12-31**
- 修复：引擎未初始化时抛出FrameworkError，逻辑层收不到

## **[0.2.5] - 2025-12-31**
- 新增：System初始化失败的引擎事件
- 修复：VersionPack加载失败没有对外抛出事件的BUG

## **[0.2.4] - 2025-12-31**
- 新增：AssetViewer增加解密版本文件功能
- 新增：资源版本比较工具
- 新增：命令行打包增加新参数copyToStreaming
- 新增：引擎报错事件FrameworkErrorEvent，方面业务层处理如网络请求失败的业务逻辑
- 优化：TaskSystem权限优化，避免过多不必要的函数暴露

## **[0.2.3] - 2025-12-01**
- 新增：网络相关逻辑与远程 Bundle 下载的重试机制
- 修复：AssetSystem 在 Remote 模式下依赖加载顺序错误的问题

## **[0.2.2] - 2025-11-20**
- 修复：重构导致的 Scene 加载失败问题

## **[0.2.1] - 2025-11-20**
- 新增：AssetSystem 增加 Resource 模式
- 优化：统一平台字符串（如 WebGL 与 WebGLPlayer）
- 优化：Remote 模式下使用主版本号
- 修复：版本号不足三位导致的错误

## **[0.2.0] - 2025-10-09**
- 新增：AssetSystem 增加新的打包模式与编译选项；统一加载方式
- 新增：引入 UniTask
- 重构：VersionPackInfo 支持加密并优化加载逻辑
- 修复：若干稳定性问题

## **[0.1.0-preview] - 2025-02-25**
- 重构：Task 模块
- 新增：Asset 模块支持远程资源，可将所有 Bundle 放置于远端
- 新增：编辑器扩展模块，可直接绘制对象，支持大部分类型
- 新增：Codec 模块，可序列化复杂类型
- 优化：Logic Type 记录方式更加稳健
- 优化：多项 UI 显示细节
- 修复：TypeUtils 数组动态创建的特定问题
- 修复：showunitylogo 构建选项被错误关闭的问题

## **[0.0.20-preview] - 2024-09-20**
- 新增：Project Builder 增加 SplitApplicationBinary 命令行参数支持
- 修复：命令行 Bool 参数解析错误

## **[0.0.19-preview] - 2024-09-10**
- 新增：Project Builder 增加 AAB 的 Split Binary 支持
- 新增：宏管理接口
- 优化：Project Builder 关联 CleanModel 与 DebugBuild 选项
- 修复：输出包命名错误

## **[0.0.18-preview] - 2024-08-29**
- 新增：Project Builder 增加 Callback（继承 IBuildCallback）
- 优化：构建完成弹窗逻辑，仅在成功时显示
- 优化：以 SynchronizationContext 替代 TaskSystem 中的主线程执行逻辑
- 修复：ListEx/DictionaryEx 在同一帧 Foreach→Add→Clear 的数据丢失问题

## **[0.0.17-preview] - 2024-08-26**
- 新增：Project Builder 增加 CreateSymbols 选项
- 新增：TaskSystem 增加 AsyncOperation 与 AsyncResult 类型的 Task
- 新增：ObjectSystem 封装 UnityEngine.Pool.CollectionPool 的部分功能

## **[0.0.16-preview] - 2024-07-31**
- 新增：TaskSystem 增加 PipelineTask
- 新增：AudioSystem 增加延迟播放参数
- 优化：Timer.NextFrame 默认使用 0 延迟
- 新增：ObjectPoolSystem 增加异步实例化接口

## **[0.0.15-preview] - 2024-07-25**
- 优化：构建输出名称自动将空格替换为下划线
- 修复：Windows 平台构建脚本错误
- 修复：命令行参数与 Unity 冲突的问题

## **[0.0.14-preview] - 2024-07-19**
- 修复：Debug 构建参数未生效的问题

## **[0.0.13-preview] - 2024-07-19**
- 新增：AAB 构建支持
- 优化：Timer 风格与其他系统统一（使用 ID）
- 修复：Action 循环中无法停止其他新增 Action 的问题

## **[0.0.12-preview] - 2024-07-17**
- 新增：构建系统增加 App Clean Build 选项
- 优化：命令行构建模式
- 修复：Mac 无法打开目录的问题
- 修复：DebugMode 空引用问题

## **[0.0.11-preview] - 2024-07-17**
- 新增：Asset Viewer 资源浏览器
- 新增：多项新的构建参数
- 新增：DebugSetting 设置项
- 优化：运行时可动态添加 Logic

## **[0.0.10-preview] - 2024-07-12**
- 修复：Setting Inspector 界面错误

## **[0.0.9-preview] - 2024-07-12**
- 新增：异常抛出开关，可关闭框架异常捕获，由项目方统一上报
- 新增：日志增加 Simple 模式，启用后使用 UnityLog
- 修复：使用 Text 组件的项目字体丢失问题

## **[0.0.8-preview] - 2024-07-09**
- 新增：AssetSystem 增加 Retain 开关
- 新增：RectTransformMoveAction（操作 anchorPosition）
- 新增：BaseUI 增加 ReplaceSprite 接口；缓存所有加载的 Asset 做统一释放
- 优化：Bundle 自动收集规则排除 Package 目录
- 优化：多项 UI 细节
- 修复：UI 同一帧关闭再打开导致生命周期错误的问题

## **[0.0.7-preview] - 2024-07-02**
- 优化：Bundle 打包增加根目录，避免冲突
- 优化：ActionSystem 增加 SetParent 并补全部分接口
- 优化：构建工具界面；修复构建完成后的报错
- 修复：GetPool 在部分情况下的异常

## **[0.0.6-preview] - 2024-06-19**
- 新增：EventSystem 拆分模块，系统可独立注册/反注册
- 优化：ActionSystem
- 修复：MovePathAction 耗时超出设定的问题

## **[0.0.5-preview] - 2024-05-31**
- 新增：内部系统改用 LitJson，移除 Newtonsoft.Json

## **[0.0.4-preview] - 2024-05-31**
- 修复：资源管理器自动释放资源导致无法再次加载的问题
- 修复：音效资源泄露问题

## **[0.0.3-preview] - 2024-05-29**
- 新增：AudioSystem
- 新增：EmptyObjectPool
- 更新：IngameDebugConsole 至最新版本
- 修复：若干稳定性问题

## **[0.0.2-preview] - 2024-05-20**
- 新增：ActionSystem
- 优化：优化已有逻辑
- 优化：调整外部访问 System 的方式
- 修复：若干稳定性问题

## **[0.0.1-preview] - 2024-04-23**
- 新增：LiteQuark 初版（包含 Asset 相关功能）
