# AssetSystem 资源管理

通过 `LiteRuntime.Asset` 加载资源、实例和场景。业务保存 Handle，使用 `await handle` 等待、`handle.Result` 读取结果，并在使用结束后调用 `Dispose()`。等待完成不会转移所有权；资源引用本身不能替代 Handle。

## 常用入口

| API | 结果与释放责任 |
| --- | --- |
| `LoadAssetHandle<T>(path, priority, ct)` | `AssetHandle<T>` 持有资源；Dispose 释放引用 |
| `InstantiateHandle(path, parent, ...)` | `GameObjectHandle` 持有实例；Dispose 销毁实例并释放 prefab 引用 |
| `LoadSceneHandle(path, parameters, priority, ct)` | `SceneHandle` 持有实际场景实例；Dispose 发起异步卸载 |
| `PreloadAssetHandle<T>(paths, priority, ct)` | `PreloadHandle<T>` 持有批量资源引用，Result 为成功标志 |
| `HasAsset(path)` / `GetVersion()` | 查询当前 Provider 的资源收录情况／版本，不取得资源所有权 |
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
var parameters = new UnityEngine.SceneManagement.LoadSceneParameters(
    UnityEngine.SceneManagement.LoadSceneMode.Additive);
var sceneHandle = LiteRuntime.Asset.LoadSceneHandle("Scenes/Battle.unity", parameters);
await sceneHandle;
if (sceneHandle.Result == null)
{
    LLog.Error("Scene load failed");
}
// 离开场景时释放；Dispose 返回不代表卸载已经完成。
sceneHandle.Dispose();
```

`SceneHandle.Result` 为 `Scene?`，失败、释放或场景失效时为空。同一 Provider 内，规范化后同一路径的并发加载会被拒绝；一次加载完成后再次 Additive 加载，可创建独立实例，各 Handle 卸载自己的实例。限制不覆盖业务直接调用 Unity 场景 API。

Single 模式替换或外部卸载后，Result 为空，成功历史状态保留；业务仍应 Dispose，框架不会重复卸载或释放引用。Unity 拒绝卸载时（例如最后一个场景），Dispose 不保证场景消失，框架保留必要资源直到确认实际卸载；没有公开的 Handle 重试入口，也不自动逐帧重试。

**需要同时加载的 Bundle 场景应使用不同文件名。** Unity 2022.3.62f3 的本地验证中，同名场景在同一个 Bundle 内会构建冲突，分到不同 Bundle 后并发装载也会冲突。Editor 直读可区分不同目录的同名场景，完整路径不能消除 Bundle 层限制。

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

AssetSystem 由 Runtime 统一关闭。关闭开始后拒绝新加载，结束未完成请求，等待原生加载收尾和场景实际卸载，再释放全部 Bundle；不等待 Handle 归还，也不受 Retention 限制。Editor 模式同样会等待已有加载和延迟交付，关闭中的迟到结果不再成功交付。

业务应先停止资源使用并释放 Handle；遗漏 Handle 不保留旧资源的可用性，实例仍由其拥有者销毁。Handle／Lease 的 Dispose 仍是同步归还，返回不代表原生卸载已经结束。系统关闭不主动执行全局 Resources.UnloadUnusedAssets 或 GC。

关闭异常会在其余收尾结束后向上传播，不自动重试或恢复。内容收尾失败时不进入最终 Bundle 卸载；最终释放失败时仍尝试其余 Bundle，此时可能已释放部分资源，不能继续使用旧系统。原生操作不结束时关闭会继续等待，没有生产超时。

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

在 Launcher 的 Asset 设置中配置：

| 配置 | 含义 |
| --- | --- |
| `AssetMode` | Editor 直接访问项目资源；Bundle 使用 AssetBundle。非编辑器下 Editor 自动转为 Bundle |
| `BundleLocater` | BuiltIn 使用本地文件；Remote 使用远端资源包 |
| `BundleRemoteUri` | 绝对基础 URI，按目录追加小写平台名和版本号前两段（如 0.4）；传未编码路径名称，已有查询和 fragment 保留，勿配置框架保留参数 t |
| `ConcurrencyLimit` | Bundle Loader 同时提交加载任务的上限，默认 5 |
| `TaskConcurrencyLimit` | 独立 Asset 任务组额度，默认 20，运行时至少为 1；覆盖版本包、Bundle、Asset 和 Scene 加载任务 |
| `EnableRetain` / `BundleRetainTime` | Bundle 零引用保留缓存开关／时间，默认 true／300 秒 |

BuiltIn 在编辑器 `EditorForceStreamingAssets` 为 true（默认）时强制选择 StreamingAssets；否则优先选择 `persistentDataPath/LiteQuark` 中存在的文件，缺失时选择 `StreamingAssets/LiteQuark`。这是按存在性选路径，persistent 文件损坏或加载失败不会再次回退。

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
- Editor 直读接受优先级参数，但不经过 Asset 组排队，也不模拟 Bundle 队列。Profiler 的 Bundle Begin 表示 Cache 接受请求，不代表实际提交顺序；Profiler 事件订阅者不得在事件内加载、释放或修改资源生命周期。

## 缓存与构建工具

Handle 释放后，资源是否立即回收取决于共享引用和 Bundle 缓存。只有 Bundle 层计算保留时间；零引用 Bundle 进入保留期，关闭 EnableRetain 时在后续清理中回收。保留期间相同路径、相同请求类型可命中缓存；不同类型分别缓存但共享 Bundle。缓存不延长已释放 Handle 的所有权。

主 Bundle 实际卸载后才释放依赖引用，依赖引用归零后开始自己的保留计时。`UnloadUnusedAssets(maxDepth)` 最多执行指定轮数的缓存清理，不是依赖图层数；它还调用 Unity 未使用资源清理和 GC。释放最后引用后调用它可能立即回收资源，不要继续使用旧引用。

版本清单 `BundleInfo.AssetList` 保存完整规范资源 key。调用 `ToJsonData()` / `ToBinaryData()` 会将 DependencyList 原地转换为 ID，输出后不要继续查询或重复输出该对象；再次输出使用重新收集或重新读取并恢复的对象。

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
