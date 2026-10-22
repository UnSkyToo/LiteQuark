# AssetSystem 资源管理

通过 `LiteRuntime.Asset` 访问。加载、预加载、实例化和场景所有权统一使用 Handle；`await handle` 只等待完成，结果从 `handle.Result` 读取。底层加载失败可能返回 `null` / `false`，取消会使等待抛出 `OperationCanceledException`。

`AssetHandle<T>`、`GameObjectHandle`、`SceneHandle` 和 `PreloadHandle<T>` 的 `Task` 属性为 `internal`，仅供框架内部组合加载流程。业务代码统一保存 Handle，通过 `await handle` 等待、`Result` 读取结果，并在使用结束后调用 `Dispose()`；等待完成不会转移资源所有权。

## 路径与模式

资源默认放在 `Assets/StandaloneAssets`。加载、实例化、预加载、场景加载和 `HasAsset` 接受两种路径：相对该目录的文件路径，例如 `Prefabs/Player.prefab`；或者以 `Assets/` 开头的完整项目路径，例如 `Assets/StandaloneAssets/Prefabs/Player.prefab`。这里的完整项目路径不是磁盘绝对路径。显式的根目录外项目路径（如 `Assets/Shared/Material.mat`）也可表达；Bundle 模式要求资源已被构建清单收录，不会因此扩大主动收集范围。

入口通过 [AssetPath](AssetPath.cs) 将资源身份统一为小写、使用 `/` 的完整项目路径。相对路径、完整路径以及大小写或斜杠不同的写法，会得到同一个 key；重复斜杠和 `.` 路径段会被归一化。必须包含文件扩展名，不会自动补全；场景路径必须以 `.unity` 结尾。空路径、`..`、磁盘绝对路径、URL、显式目录路径及不支持的 `Packages/` 路径等非法输入会使加载入口同步抛出 `ArgumentException`；`HasAsset` 对非法输入返回 `false`。合法但不存在的资源仍按加载失败处理，不能将路径校验通过视为资源存在。

普通 Bundle 资源按完整资源路径加载，不按短文件名查找。场景使用规范 key 定位资源和限制并发，再使用 Bundle 或 AssetDatabase 返回的实际项目路径加载，保留实际加载路径的大小写。资源 key 的小写规则不适用于任意磁盘路径、远端 URL 或 Bundle 文件位置。

版本文件继续以压缩后的相对路径为主，JSON 字段、依赖 ID 字符串、UTF-8 编码和现有加密方式保持不变。普通 Bundle 使用其逻辑 `BundlePath` 去掉 `.ab` 后的目录作为前缀：例如 `a/b/c.ab` 中的 `a/b/c/d.png` 存为 `d.png`，读取时补回 `a/b/c/`。压缩只截去开头匹配的目录前缀，不删除路径中间再次出现的同名目录；不匹配的普通相对路径会报错，已有的完整 `Assets/...` 路径表示仍受支持。

恢复时按 `Assets/` 目录边界识别完整项目路径，因此 `assets.json`、`assets_icon.png`、`assetsbackup/icon.png` 等普通相对路径会正常补回 Bundle 前缀。**已知目录歧义（暂缓处理）：**如果压缩后的相对路径本身以 `assets/` 开头（不区分大小写），仍无法与完整项目路径区分。例如 `data.ab` 中的 `data/assets/icon.png` 存为 `assets/icon.png` 后，恢复时会跳过 `data/`。本轮仅修复普通名称的前缀误判，未扩展存储表示；暂时应避免产生这类压缩路径，后续单独处理该歧义。

只有根级 `default.ab` 使用空前缀：根目录的 `config.json` 原样存储并恢复为 `config.json`；真实 `default` 子目录中的 `default/config.json` 保留该目录，二者不会混淆。`a/default.ab` 等嵌套 Bundle 仍使用普通目录前缀。这一规则与 HashMode、FlatMode 产生的物理 Bundle 文件名无关。

读取时先恢复资源路径和依赖路径，再由 `Initialize()` 建立完整规范 key 索引；建立索引不会再次改写 `BundleInfo.AssetList`。公开的 `VersionPackInfo.GetBundleInfoFromAssetPath` 接受相对资源根目录的路径及完整项目路径。`ToJsonData()`、`ToBinaryData()` 保留原地转换方式，会将原对象改成存储表示；应将写出视为该对象本轮使用的结束，不继续用它查询或重复输出。需要再次输出时，应使用重新收集或重新读取并恢复的对象。

**旧 `default.ab` 清单的兼容限制：**升级到本次路径恢复逻辑时，应从源资源重新收集并生成包含 `default.ab` 的旧版本清单。旧实现可能将根目录的 `icon.png` 和真实子目录中的 `default/icon.png` 都存成 `icon.png`，已经丢失的信息无法自动恢复；仅把旧清单读入后再次导出不能修复这种歧义。旧版字符串替换误删了路径中间目录的清单同样需要从源资源重新生成。正确编码的普通 Bundle 旧清单仍可读取，但不能把字段格式未变理解为所有旧数据都可无损迁移。新生成的 `default.ab` 清单应与修复后的读取逻辑配套使用，旧读取器仍会错误补上 `default/`。

在 Launcher 的 Asset 设置中配置：

| 配置 | 含义 |
| --- | --- |
| `AssetMode` | `Editor` 直接访问项目资源；`Bundle` 使用 AssetBundle。非编辑器下 Editor 自动转为 Bundle |
| `BundleLocater` | `BuiltIn` 优先读取 persistentDataPath/LiteQuark，再回退到 StreamingAssets/LiteQuark；`Remote` 使用远端资源包 |
| `BundleRemoteUri` | 远程基础地址；运行时拼接小写平台名和版本号前两段（如 `0.4`）目录 |
| `ConcurrencyLimit` | BundleLoader 同时提交的 Bundle 加载任务数量限制，默认 5 |
| `TaskConcurrencyLimit` | 独立 Asset 任务组的并发额度，默认 20，包含版本包、Bundle、Asset 和 Scene 加载任务 |
| `EnableRetain` | 启用 Bundle 保留缓存，默认 true |
| `BundleRetainTime` | Bundle 引用归零后的保留时间，默认 300 秒 |

发布前使用 `Lite > Builder` 生成 Bundle 和版本文件；资源随包发布时放入对应 StreamingAssets。在编辑器中测试 BuiltIn 模式时，`EditorForceStreamingAssets` 默认为 true，会强制从 StreamingAssets 读取。Bundle 初始化会检测依赖环，检测失败或版本文件加载失败时不会完成系统初始化。Resource 模式已移除。

AssetSystem 在 Provider 初始化前注册 `LiteConst.TaskGroupId.Asset`（-2），因此版本包加载也使用该组。Asset 组与普通业务的 Default 组额度独立；BundleLoader 仍管理自己的队列、并发和共享请求提权，两层限制各自生效。Loader 已提交但尚未获 Asset 组准入的任务也占用 Loader 的提交名额。额度统计逻辑任务占位，不保证取消后的 Unity 原生操作已经停止。Editor 模式也注册该组，但直接加载路径不通过该组排队。

## 加载与所有权

示例使用 `LiteQuark.Runtime`、`Cysharp.Threading.Tasks` 和 `UnityEngine`；含 `await` 的片段放在异步方法中。示例路径需替换为项目实际资源。

```csharp
using var handle = LiteRuntime.Asset.LoadAssetHandle<TextAsset>("Config/game.json");
await handle;
if (handle.Result != null)
{
    var json = handle.Result.text;
    // 在 Handle 有效期内解析或复制所需数据。
}
```

资源要继续用于渲染、音频或实例时，将 Handle 保存在拥有者字段，在 `Dispose()`、`OnDestroy()` 或关闭界面时释放。不能在 `using` 结束后仅保留资源引用；手动从 prefab 实例化时也必须维持 prefab Handle 的寿命。

```csharp
var handle = LiteRuntime.Asset.InstantiateHandle("Prefabs/Enemy.prefab", parent: null);
await handle;
var enemy = handle.Result; // 加载失败时为 null
// 使用实例；实际业务应保存 handle，直到实例不再需要。
handle.Dispose();
```

`GameObjectHandle.Dispose()` 销毁实例并释放内部 prefab 引用。不要另行销毁实例后遗忘 Handle。

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
// 离开该场景时调用；持有期间不要提前释放。
sceneHandle.Dispose();
```

`SceneHandle.Result` 为 `Scene?`，加载失败、Handle 已释放或场景已失效时返回 `null`；非空时可通过 `.Value` 取得实际 Scene。`Dispose()` 发起该场景的异步卸载，返回不代表卸载完成。同一 Provider 内，规范化后同一路径的并发加载会被拒绝，包括相对路径与完整路径混用的请求；不同目录下的同名场景可以区分。前一次加载完成后再次以 Additive 加载同一路径，会创建独立实例，各 Handle 持有并卸载自己的 Scene 实例。并发限制不覆盖外部直接调用 Unity 场景加载 API 的请求。通过 Single 模式或外部调用卸载场景后，框架会监听实际卸载事件，自动结束对应场景条目并归还其 Bundle 引用，`SceneHandle.Result` 返回 `null`。业务仍应在持有者生命周期结束时调用 `Dispose()`；此时不会重复卸载场景或释放资源引用。归还引用不代表共享 Bundle 立即卸载，实际回收仍遵循引用计数和缓存策略。Editor 和 Bundle 模式共用这一场景收尾逻辑。

场景卸载先启动 Unity 的卸载操作，再通过 TaskSystem 的 `AsyncOperation` 便利入口观察完成；该包装任务进入无限额的 Flow 组，不等待默认组空位。Flow 不改变底层卸载的异步完成语义。

框架能区分同名场景的完整路径，不代表底层 Bundle 可以同时装载。Unity `2022.3.62f3` 的本地验证中，将两个 `Battle.unity` 放入同一个 Bundle 会因内部 `BuildPlayer-Battle.sharedAssets` 重名而构建失败；拆到不同 Bundle 后，同时装载仍触发“另一个包含相同文件的 AssetBundle 已加载”的错误。需要同时加载的 Bundle 场景应使用不同文件名。Editor 直读模式下不同目录同名场景可以并发加载；Bundle 构建层的限制与校验另行处理，不能仅靠完整路径解析消除。

```csharp
var paths = new[] { "UI/IconA.png", "UI/IconB.png" };
using var preload = LiteRuntime.Asset.PreloadAssetHandle<Sprite>(paths);
await preload;
if (!preload.Result)
{
    foreach (var path in preload.FailedPaths)
    {
        LLog.Error("Preload failed: {0}", path);
    }
}
// 将 preload 保留到预加载资源不再需要；使用资源时另取自己的 AssetHandle。
```

预加载先解析整批输入，再对规范化后的完整路径去重并开始加载；任意输入非法时同步抛出异常，不会启动这批预加载。提供 `TotalCount`、`LoadedCount`、`FailedCount` 和 `FailedPaths`，其中失败路径使用规范化后的完整 key。它持有批量资源引用，不直接提供资源对象；释放它会释放内部 Handle。

## 加载优先级

`LoadAssetHandle`、`PreloadAssetHandle`、两个 `InstantiateHandle` 重载和 `LoadSceneHandle` 均接受 `priority` 参数，类型为 [AssetLoadPriority](AssetLoadPriority.cs)：`Low`、`Normal`、`High`，默认均为 `Normal`。参数位于可选的 `ct` 之前，建议使用命名参数：

```csharp
// 后台预加载。业务持有该 Handle，直到不再需要这批资源。
var preload = LiteRuntime.Asset.PreloadAssetHandle<GameObject>(
    backgroundPaths, priority: AssetLoadPriority.Low, ct: cancellationToken);

// 玩家当前需要的界面。实例化入口将优先级传给内部 prefab 加载。
var window = LiteRuntime.Asset.InstantiateHandle(
    "UI/Inventory.prefab", parent: uiRoot,
    priority: AssetLoadPriority.High, ct: cancellationToken);
```

`backgroundPaths`、`uiRoot` 和 `cancellationToken` 由调用方提供；两个 Handle 仍须在使用结束后 `Dispose()`。预加载将同一优先级传给每个子请求，不固定为低优先级。

- 优先级只决定 Bundle Loader 尚未提交到底层 TaskSystem 的任务顺序。优先级高的先出队；同级按首次入队顺序执行，提权保留原入队序号。已提交的加载不抢占、不重启，也不因提权突破 Asset 设置的 `ConcurrencyLimit`。
- 请求优先级传给主 Bundle 及全部依赖。共享加载在本轮收到更高优先级请求时会向尚未就绪的依赖传播提权，即使主 Bundle 自身已启动或物理加载已完成。依赖不额外提高一级；读取 Asset 或加载 Scene 前仍等待全部必要 Bundle 成功。
- 本轮有效优先级只升不降。后来的低优先级请求，以及高优先级 Handle 的取消或释放，都不会降低本轮优先级。加载失败后重试会以新请求重新初始化优先级；取消和迟到结果的所有权规则不变。
- 优先级和同级 FIFO 均不保证 Asset 请求的完成顺序。TaskSystem 另按任务组限制并发，没有跨组总上限；版本包和 Bundle 任务以 `TaskPriority.Urgent` 提交到独立 Asset 组，Bundle 就绪后的 Asset/Scene 任务以 `Normal` 提交到同一组，均受 `TaskConcurrencyLimit` 约束，不占用 Default 额度。Bundle Loader 自身的并发限制仍然保留。`AssetLoadPriority` 不等同于 `TaskPriority`，不会改变已提交任务的固定优先级或获得绕限权限，也不保证整条加载链优先。Asset 组内部仍共享额度；持续高优先级请求可能延迟低优先级任务，目前没有优先级老化或保底额度。参见 [TaskSystem 分组调度](../Core/Async/Task/README.md)。
- Editor 直读模式接受该参数，但不模拟 Bundle 优先级队列。验证调度顺序时使用 Bundle 模式，并观察 Loader 实际提交任务的时机；Profiler 的 Bundle Begin 事件表示 Cache 接受加载，不能用来判断实际开始顺序。

资源加载 Profiler 事件只用于编辑器观测，订阅者不得在事件中发起加载、释放或修改资源生命周期。

## 释放与迁移

- 每个加载请求持有自己的 Handle，重复 `Dispose()` 不会重复释放该 Handle 的引用。
- Bundle 模式的 Asset 请求在被接受时取得内部 Lease，覆盖等待 Bundle 和资源加载的阶段；加载成功后将同一份 Asset Lease 转交给 Handle，失败时归还引用。场景请求提前取得的 Bundle Lease 则在 Bundle 加载成功后转交场景条目，按场景生命周期归还。
- `UIUtils.ReplaceSpriteHandle(...)` 等工具返回的 Handle 也由调用者保管，在图片不再使用时释放；不要忽略返回值。
- 加载期间取消或释放 Handle，不保证底层 Unity 加载立即停止；内部请求引用保留到加载完成通知，再由失败路径或 Handle 对迟到结果的清理归还。用 `try/finally` 或 `using` 保证退出时释放。
- Bundle 模式仅在 Bundle 层计算保留时间。最后一份 Asset Lease 释放、资源引用归零时，资源条目立即归还主 Bundle 引用，不再等待当前回调派发结束，并保留已加载对象的索引；条目和索引随所属 Bundle 卸载一起清除。仍有未交付请求或有效 Handle 的资源会继续持有 Bundle 引用。回调中释放最后一份引用后调用 `UnloadUnusedAssets()`，可以在该回调返回前回收 Bundle。
- Bundle 引用归零后进入保留期；期间再次以相同路径和相同请求类型加载已缓存资源，会重新取得引用并直接交付已有对象，无需再次发起 Asset 加载。不同请求类型使用独立的资源缓存条目，包括存在继承关系的类型，但仍共享所属 Bundle。关闭 `EnableRetain` 时，零引用 Bundle 在后续清理时回收。保留缓存不会延长已释放 Handle 的所有权，业务仍须用有效 Handle 覆盖资源使用期。
- 依赖 Bundle 继续由主 Bundle 持有引用，主 Bundle 实际卸载后才归还；依赖引用归零时再开始自己的保留计时。
- 原 `AssetRetainTime` 配置已移除，只需配置 `BundleRetainTime`。主 Bundle 无其他引用时，不再先等待一段独立的 Asset 保留时间。
- `UnloadUnusedAssets(maxDepth: 5)` 最多执行指定轮数的缓存清理，以处理依赖释放；它不是精确的依赖图层数限制。实现还调用 Unity 的未使用资源清理和 GC，不宜逐帧执行。
- 旧的 AssetSystem 回调加载、非 Handle 加载、按路径/对象释放、手动场景卸载接口已移除。实例化改用 `InstantiateHandle`，批量预加载改用 `PreloadAssetHandle<T>`。
- 原先的 `var result = await handle.Task` 改为 `await handle; var result = handle.Result;`，并保持 Handle 覆盖资源使用期。不要通过 `GetAwaiter().GetResult()` 同步读取尚未完成的加载；将调用链改为异步等待。
- 加入优先级参数后，原有按位置传入取消令牌的调用（如 `LoadAssetHandle<T>(path, token)`）应改为 `LoadAssetHandle<T>(path, ct: token)`，或显式传入 `priority` 后再传 `ct`。预加载、实例化和场景加载入口同样适用。

战斗模块在 `LiteNexusDataManager.Startup()` 中等待配置加载，再读取数据库；运行时同步访问 `LiteNexusConfig.Instance` 前必须完成该初始化，初始化失败或关闭数据管理器时释放配置 Handle。编辑器工具仍使用 AssetDatabase 访问配置。

完整签名见 [AssetSystem.cs](AssetSystem.cs)，所有权实现见 [Handle](Handle)。
