# AssetSystem 资源管理

通过 `LiteRuntime.Asset` 加载资源、实例和场景。业务保存 Handle，使用 `await handle` 等待、`handle.Result` 读取结果，并在使用结束后调用 `Dispose()`。等待完成不会转移所有权；资源引用本身不能替代 Handle。

## 常用入口

| API | 结果与释放责任 |
| --- | --- |
| `LoadAssetHandle<T>(path, priority, ct)` | `AssetHandle<T>` 持有资源；Dispose 释放引用 |
| `InstantiateHandle(path, parent, ...)` | `GameObjectHandle` 持有实例；Dispose 销毁实例并释放 prefab 引用 |
| `LoadSceneHandle(path, priority, ct)` | 以 Additive 加载业务场景，`SceneHandle.Dispose()` 发起该实例的卸载 |
| `PreloadAssetHandle<T>(paths, priority, ct)` | `PreloadHandle<T>` 持有批量资源引用，Result 为成功标志 |
| `HasAsset(path)` / `GetVersion()` | 查询当前 Manager 的资源收录情况／版本，不取得资源所有权 |
| `UnloadUnusedAssets(maxDepth: 5)` | 清理零引用缓存；不宜逐帧调用 |

加载入口的 priority 默认 `Normal`，ct 默认空令牌。建议使用 `priority:`、`ct:` 命名参数。完整重载见 [AssetSystem.cs](AssetSystem.cs)。示例使用 `LiteQuark.Runtime`、`Cysharp.Threading.Tasks` 命名空间；调用在 Unity 主线程进行，含 await 的示例放在异步方法内，路径替换为实际资源。

```csharp
using var handle = LiteRuntime.Asset.LoadAssetHandle<UnityEngine.TextAsset>("Config/game.json");
await handle;
if (handle.State == AssetHandleState.Failed)
{
    LLog.Error("Load failed: {0}, {1}", handle.Error.ResourceKey, handle.Error.Message);
    return;
}
var json = handle.Result.text;
// 在 Handle 有效期内解析或复制所需数据。
```

用于渲染、音频等长期用途时，把 Handle 保存在拥有者字段，在 `OnDestroy()`、关闭界面或业务 Dispose 时释放。不能在 using 结束后仅保留资源引用；手动实例化 prefab 时也要维持 prefab Handle 的寿命。

```csharp
var handle = LiteRuntime.Asset.InstantiateHandle("Prefabs/Enemy.prefab", parent: null);
await handle;
var enemy = handle.Result; // 普通加载失败时为 null
// 保存 handle，直到实例不再需要。
handle.Dispose();
```

不要另行销毁实例后遗忘 Handle。`UIUtils.ReplaceSpriteHandle(...)` 等工具返回的 Handle 同样由调用者保存和释放。

## 请求状态与错误

四类 Handle 提供 `State`、`Error`、`IsDone`、`IsDisposed` 和 `Result`。状态从 `Pending` 单次进入下列终态；缓存命中可能在加载入口返回前完成。`IsDone` 表示请求已结束，不代表底层原生操作或迟到资源清理已结束。

| 情况 | 状态、等待与所有权 |
| --- | --- |
| 成功 | `Succeeded`，Error 为 null，Handle 持有结果 |
| 普通加载失败 | `Failed`，Error 非空；await 正常结束，Result 为 null / false |
| 等待中 token 取消或 Dispose | 立即成为 `Cancelled`，Error 为 null；await 抛 `OperationCanceledException` |
| 成功或失败后 Dispose | State / Error 保留，IsDisposed 为 true；释放持有的资源 |
| 成功后 token 取消 | 保留成功及所有权，业务仍需 Dispose |

- `IsDisposed` 只表示显式 Dispose，token 取消不将它设为 true。重复 Dispose 不重复释放。
- 资源、实例和场景 Handle 释放后 Result 为空；预加载的 bool Result 保留批次结论。State 不代替当前资源有效性检查。
- 取消仅结束当前 Handle 的等待，不保证共享加载或 Unity 原生操作停止；框架清理迟到结果。使用 using 或 try/finally 保证释放。
- 实例化过程中抛出的异常会由 await 抛出，同时保存在 Error；非法入口参数同步抛异常。不要同步读取尚未完成的加载。
- 完成状态和所有权在恢复 await 续体前确定，续体可以立即 Dispose 或发起新请求。

[AssetError](Base/AssetError.cs) 提供不可变的 `ResourceKey`、`Message` 和可为空的原始 `Exception`。ResourceKey 指向实际失败的资源或依赖 Bundle，不保证等于请求路径；Message 是诊断文本，不作为稳定分类标识。共享失败可能返回同一个错误对象，重试不会改写旧 Handle 的错误，已取消请求也不会被迟到失败覆盖。

## 场景与预加载

```csharp
var sceneHandle = LiteRuntime.Asset.LoadSceneHandle("Scenes/Battle.unity");
await sceneHandle;
if (sceneHandle.Result == null)
{
    LLog.Error("Scene load failed");
}
// 离开场景时释放；Dispose 返回不代表卸载已经完成。
sceneHandle.Dispose();
```

启动场景常驻。单场景项目可以直接通过 Prefab Handle 管理内容。有独立关卡时，只将业务场景作为加载目标；请求已在场的启动场景会得到 Failed Handle。加载业务场景会保留当前活动场景，随关卡退场的动态对象应放到该关卡的根节点下。

`SceneHandle.Result` 为 `Scene?`，失败、释放或场景失效时为空。在同一轮 Runtime 内，从请求受理到场景实际卸载，该规范化路径一直被占用。重复请求立即得到 Failed Handle，原请求继续。加载失败且未留下场景时可以立即重试。不同路径可以同时加载。

**需要同时加载的 Bundle 场景应使用不同文件名。** Unity 2022.3.62f3 的本地验证中，同名场景在同一个 Bundle 内会构建冲突，分到不同 Bundle 后并发装载也会冲突。Simulate 可区分不同目录的同名场景，完整路径不能消除 Bundle 层限制。

```csharp
var paths = new[] { "UI/IconA.png", "UI/IconB.png" };
using var preload = LiteRuntime.Asset.PreloadAssetHandle<UnityEngine.Sprite>(paths);
await preload;
if (!preload.Result)
{
    foreach (var path in preload.FailedPaths)
    {
        LLog.Error("Preload failed: {0}", path);
    }
}
// 持有 preload 到预加载资源不再需要；实际使用资源时另取自己的 AssetHandle。
```

预加载先校验整批路径，非法输入同步抛异常且不启动该批加载；按规范 key 去重。提供 `TotalCount`、`LoadedCount`、`FailedCount`、`FailedPaths` 和 `Errors`。FailedPaths 使用规范 key，Errors 按索引对应，错误中的 ResourceKey 可能指向依赖 Bundle。普通批次失败的 Error 为首个失败原因；部分失败仍持有成功项直到 Dispose。取消后不再增加迟到结果的计数或错误。预加载不直接提供资源对象。

## 系统关闭

业务场景在场时也可以调用 Restart 或 Shutdown。已交付的场景由业务通过 SceneHandle 退场，可以在游戏入口的 DisposeAsync 中释放。资源系统关闭会释放旧 Bundle，仍在场的关卡可能失去材质等依赖。跨轮重开同一路径时，业务须释放旧 Handle，确认旧场景已实际卸载，再发起新请求。只使用 Prefab 的单场景项目可直接请求重启。

关闭开始后拒绝新请求。Asset 释放时取消排队的工作。Scene Cache 开始释放时，仍在等待交付的 SceneHandle 立即进入 Failed，业务可以结束等待。Cache 持有 Bundle 直到原生读取完成，并对迟到场景发起卸载、等待已发起的卸载结束。Unity 拒绝场景卸载时会记录错误。Loader 释放未交付的容器，Cache 释放已接收的容器。上层实例完成延迟销毁后卸包。加载失败由对应 Handle 的 Error 报告，关闭时停止下载或卸包的异常由本次 Shutdown 报告。

业务应先停止使用旧资源并释放 Handle。遗漏 Handle 和缓存保留期均不会阻止卸包。应用退出回调只尽力发起清理，跨帧操作可能随进程结束而中断。

## 路径与模式

资源默认放在 `Assets/StandaloneAssets`。加载入口接受带扩展名的根目录相对路径（`Prefabs/Player.prefab`）或完整项目路径（`Assets/StandaloneAssets/Prefabs/Player.prefab`）；场景须以 `.unity` 结尾。根目录外的 `Assets/...` 路径也可表达，但 Bundle 模式要求资源已被构建清单收录，不会因此扩大主动收集范围。

[AssetPath](Base/AssetPath.cs) 将资源 key 统一为小写及 `/`，归一化重复斜杠和 `.` 段。空路径、`..`、磁盘绝对路径、URL、目录路径等非法输入使加载入口同步抛 `ArgumentException`，HasAsset 返回 false。路径合法不代表资源存在。资源 key 的小写规则不适用于任意磁盘路径或远端 URL。

- 以 `Assets/` 开头的输入按完整项目路径解释，不区分大小写。加载根目录内名为 assets 的子目录时，使用完整路径，例如 `Assets/StandaloneAssets/assets/demo/icon.png`。
- `packages/demo/probe.txt` 相对资源根目录，不能用于访问真正的 UPM 包。框架不支持真正 `Packages/...` 资源的显式收集或直接加载；符合 Unity 构建条件的引用依赖可由 Unity 隐式打包。
- Bundle 内按完整资源路径寻址；实际场景加载路径保留源路径大小写。

| 路径工具 | 契约 |
| --- | --- |
| `ToProjectPath(input)` | 相对路径补资源根目录，允许目录，保留大小写，返回项目路径而非磁盘绝对路径 |
| `Resolve(input)` / `TryResolve` | 按相同根目录规则解析资源文件，要求扩展名，生成规范 key |
| `ResolveProjectAssetPath(input)` | 只接受完整 Assets 资源文件路径并生成 key |
| `NormalizeProjectPath(input)` | 规范化已知完整项目路径，允许目录，保留大小写 |
| `ToRootRelativePath(projectPath)` | 根内返回相对路径，根外保留完整项目路径，保留大小写；根目录自身、null 和空字符串返回空字符串；null 数组抛异常，空数组合法 |
| `NormalizeBundlePath(path)` | 规范化 Bundle 逻辑路径的斜杠、首尾分隔符和大小写，不补资源根目录 |

ToProjectPath 不恢复源文件真实大小写。ToRootRelativePath 用于作者数据，不接受磁盘绝对路径或真正的 UPM 路径。通用文件路径与 URI 操作见 [框架指南](../../README.md#通用路径与文件操作)。

AssetSystem 在初始化时注册 Asset 任务组，按配置和运行环境选择 IBundleProvider，再将后端传给 AssetManager。后端创建失败（例如非法模式或无效 Remote URI）通过返回的 UniTask 报错，await 时抛出原始异常；任务组重复注册等错误仍在调用时同步抛出。AssetManager 按清单定位 Bundle，协调包及其依赖的加载。每个 AssetBundleCache 创建并持有本包的 AssetInfoCache 和 SceneInfoCache。SimulateBundleProvider、BuiltInBundleProvider、RemoteBundleProvider 负责获取清单和 Bundle。

在场景 Launcher 的 **Asset** 模块设置资源参数。资源模式和 CDN 各有 Release 默认值和可选的 Development 覆盖。并发、重试和缓存只配置一次，运行时统一从 `LiteRuntime.Setting.Asset` 读取。

| 配置 | 含义 |
| --- | --- |
| `BundleMode` | 示例的 Development 使用 Simulate，Release 使用 BuiltIn。Simulate 在编辑器中使用项目资源验证 Bundle 管理流程；BuiltIn 使用本地文件；Remote 使用远端资源包。非 Editor 下 Simulate 自动按 BuiltIn 加载 |
| `BundleRemoteUri` | 绝对基础 URI，按目录追加小写平台名和版本号前两段（如 0.4）；传未编码路径名称，已有查询和 fragment 保留，勿配置框架保留参数 t |
| `ConcurrencyLimit` | Bundle Loader 同时提交加载任务的上限，默认 5 |
| `TaskConcurrencyLimit` | 独立 Asset 任务组额度，默认 20，运行时至少为 1；覆盖版本包、Bundle、Asset 和 Scene 加载任务 |
| `EnableRetain` / `BundleRetainTime` | Bundle 零引用保留缓存开关／时间，默认 true／300 秒 |

BuiltIn 在编辑器 `EditorForceStreamingAssets` 为 true（默认）时强制选择 StreamingAssets；否则优先选择 `persistentDataPath/LiteQuark` 中存在的文件，缺失时选择 `StreamingAssets/LiteQuark`。这是按存在性选路径，persistent 文件损坏或加载失败不会再次回退。

编辑器中默认使用 `BundleMode = Simulate` 可复用真实 Bundle 的依赖持有、排队、缓存、Lease、Retention 和关闭流程，无需预先构建资源包。版本任务执行时通过 Editor 注册的收集入口，按当前构建目标取得清单和保留大小写的源路径快照；资源必须被清单收录。收录、分包和依赖是本次初始化的快照，修改这些内容后重新初始化。Simulate 的 Provider、Task 和 Resource 保留在 Runtime 并受 `UNITY_EDITOR` 保护，只消费快照，不依赖 Editor Collector 类型。

收集配置优先读取 `Assets/Editor/ResSetting.asset`，不存在时查找项目中的 ResCollectorSetting；仍不存在则使用默认分包规则。Simulate、构建收集和 Bundle Viewer 均不会自动创建配置。只有实际修改文件夹 Inspector 的 Individual Bundle 选项或通过 Create 菜单明确创建配置时才写入资产。默认 Collector 在首次收集时读取并复制分包规则，因此构建前和收集步骤前的回调修改配置仍会生效；清单缓存期间不重新读取，清理缓存后再次收集时刷新规则。显式传入规则的 Collector 在构造时复制规则，清理缓存后也继续使用该副本。

Provider 创建版本和 Bundle 任务，并通过 TaskSystem.AddTask 提交到 Asset 组后返回。版本任务成功结果为已初始化的 VersionPackInfo；Loader 在 Tick 中观察 Bundle 任务的 IsDone，通过 GetBundle 接收成功容器或从 Error 取得加载错误，再向 Cache 交付。关闭时各任务按当前阶段响应停止请求，已经提交的本地读盘继续完成，远端下载可以中止，成功但尚未交付的容器由 Loader 释放。

Cache 收到主包后即由 Bundle 字段持有；依赖未就绪时仍处于 Loading，不启动资源读取。正常加载等主包和依赖全部返回后才发布成功或失败，失败或关闭时由 Cache 释放已接收的主包。

同一逻辑 Bundle 的共享请求由 Cache 合并；Loader 按逻辑 BundlePath 定位调度项，每项只向一个接收方交付，不按物理文件路径合并或分发共享容器。

Simulate 的版本和 Bundle 任务进入 Asset 组、使用 Urgent；Asset 和 Scene 使用 Normal。Bundle／Asset 任务执行后在下一次任务 Tick 交付，场景使用 Editor 异步加载。所有模式共用 AssetManager，资源 Lease 必须持有 Cache，场景 Cache 必须持有 Bundle Lease；Simulate 不提供随机交付延迟。

模拟清单关闭 HashMode／FlatMode，不读写版本文件或 Bundle 文件，Hash／Size 不代表构建产物，Bundle 内存统计为零。释放模拟容器不销毁 AssetDatabase 共享对象，因此模拟不保证复现原生 Bundle 卸载后的对象失效，也不验证物理命名、平台构建、压缩、网络或原生同名场景限制；这些仍需 BuiltIn／Remote 验证。Simulate 在非 Editor 环境（包括 Development Build）自动按 BuiltIn 定位，配置值不被改写；仍需构建并部署随包资源。远端部署须明确选择 Remote。

发布前通过 `Lite > Builder` 生成资源包和版本文件，随包资源放入对应 StreamingAssets。版本文件失败或 Bundle 依赖环会使系统初始化失败。URI 能正确组成不代表所有 Player 平台的读取链都经过验证。

## 加载优先级

```csharp
var preload = LiteRuntime.Asset.PreloadAssetHandle<UnityEngine.GameObject>(
    backgroundPaths, priority: AssetLoadPriority.Low, ct: cancellationToken);
var window = LiteRuntime.Asset.InstantiateHandle(
    "UI/Inventory.prefab", parent: uiRoot,
    priority: AssetLoadPriority.High, ct: cancellationToken);
// backgroundPaths、uiRoot、cancellationToken 由业务提供；两个 Handle 均需最终 Dispose。
```

- `AssetLoadPriority` 为 Low、Normal、High，决定 Bundle Loader 尚未提交任务的顺序；同级 FIFO，提权保留入队顺序，已提交加载不抢占或重启。
- 优先级传给主 Bundle 及依赖；共享加载周期内只升不降，高优先级请求取消也不降级。失败后重新请求会重新确定优先级。预加载每个子请求使用传入值。
- 优先级不保证完成顺序或整条加载链优先，也不绕过并发限制。持续高优先级请求可能使低优先级请求饥饿。
- Loader 提交额度和 Asset 任务组额度分别生效；已提交但仍等待任务组准入的任务也占 Loader 名额。它们不占业务 Default 组，但逻辑额度不保证取消后的原生操作停止。
- `AssetLoadPriority` 与 [TaskPriority](../Core/Async/Task/README.md#优先级与执行时序) 独立，不修改已提交任务的优先级。Asset 组中版本包和 Bundle 任务以 Urgent 提交，Asset/Scene 任务以 Normal 提交。
- 三种 Locater 都经过相同的 Loader 队列和 Asset 任务组。Profiler 的 Bundle Begin 表示 Cache 接受请求，不代表实际提交顺序；Profiler 事件订阅者不得在事件内加载、释放或修改资源生命周期。

## 缓存与构建工具

Handle 释放后，资源是否立即回收取决于共享引用和 Bundle 缓存。只有 Bundle 层计算保留时间；零引用 Bundle 进入保留期，关闭 EnableRetain 时在后续清理中回收。保留期间相同路径、相同请求类型可命中缓存；不同类型分别缓存但共享 Bundle。缓存不延长已释放 Handle 的所有权。

主 Bundle 实际卸载后才释放依赖引用，依赖引用归零后开始自己的保留计时。`UnloadUnusedAssets(maxDepth)` 最多执行指定轮数的缓存清理，不是依赖图层数；它还调用 Unity 未使用资源清理和 GC。释放最后引用后调用它可能立即回收资源，不要继续使用旧引用。

版本清单 `BundleInfo.AssetList` 保存完整规范资源 key。内存中的 `DependencyList` 保存 Bundle 路径，JSON 和二进制清单按相同顺序写入字符串 ID。可以对同一份清单重复或交替调用 `ToJsonData()` 和 `ToBinaryData()`，导出后继续查询原清单。

**旧相对路径清单必须从源资源重新构建，与新读取器配套使用。** 读入旧清单再导出不能可靠迁移，解析成功也不能证明兼容。其他升级步骤见 [CHANGELOG](../../CHANGELOG.md)。

Bundle 物理文件名由 `VersionPackInfo` 的构建／加载路径接口计算：

| 配置（逻辑名为 ui/icons.ab） | 构建文件名 | 加载文件名 |
| --- | --- | --- |
| FlatMode、HashMode 关闭 | `ui/icons.ab` | `ui/icons.ab` |
| 仅 FlatMode | `ui_icons.ab` | `ui_icons.ab` |
| 仅 HashMode | `ui/icons.ab` | `ui/icons_<hash>.ab` |
| FlatMode、HashMode 开启，SecurityMode 关闭 | `ui_icons.ab` | `ui_icons_<hash>.ab` |
| FlatMode、HashMode、SecurityMode 开启 | `ui_icons.ab` | `<hash>.ab` |

FlatMode 下 `a/b_c.ab` 与 `a_b/c.ab` 会碰撞，构建映射报错；调整源目录／命名或关闭 FlatMode。HashMode 不能消除构建阶段的碰撞。

`Lite > Asset > Bundle Viewer` 显示真实源路径。Alt 选择资源定位该资源，选择 Bundle 定位首个资源所在目录，空节点不定位。
