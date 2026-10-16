# AssetSystem 资源管理

通过 `LiteRuntime.Asset` 访问。加载、预加载、实例化和场景所有权统一使用 Handle；`await handle` 只等待完成，结果从 `handle.Result` 读取。底层加载失败可能返回 `null` / `false`，取消会使等待抛出 `OperationCanceledException`。

`AssetHandle<T>`、`GameObjectHandle`、`SceneHandle` 和 `PreloadHandle<T>` 的 `Task` 属性为 `internal`，仅供框架内部组合加载流程。业务代码统一保存 Handle，通过 `await handle` 等待、`Result` 读取结果，并在使用结束后调用 `Dispose()`；等待完成不会转移资源所有权。

## 路径与模式

资源放在 `Assets/StandaloneAssets`，调用时使用相对该目录的完整文件路径，例如 `Prefabs/Player.prefab`。路径统一为小写和 `/`，不会自动补扩展名。

在 Launcher 的 Asset 设置中配置：

| 配置 | 含义 |
| --- | --- |
| `AssetMode` | `Editor` 直接访问项目资源；`Bundle` 使用 AssetBundle。非编辑器下 Editor 自动转为 Bundle |
| `BundleLocater` | `BuiltIn` 优先读取 persistentDataPath/LiteQuark，再回退到 StreamingAssets/LiteQuark；`Remote` 使用远端资源包 |
| `BundleRemoteUri` | 远程基础地址；运行时拼接小写平台名和版本号前两段（如 `0.4`）目录 |
| `ConcurrencyLimit` | Bundle 加载并发，默认 5 |
| `EnableRetain` | 启用保留缓存，默认 true |
| `AssetRetainTime` / `BundleRetainTime` | 默认 120 / 300 秒 |

发布前使用 `Lite > Builder` 生成 Bundle 和版本文件；资源随包发布时放入对应 StreamingAssets。在编辑器中测试 BuiltIn 模式时，`EditorForceStreamingAssets` 默认为 true，会强制从 StreamingAssets 读取。Bundle 初始化会检测依赖环，检测失败或版本文件加载失败时不会完成系统初始化。Resource 模式已移除。

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
if (!sceneHandle.Result)
{
    LLog.Error("Scene load failed");
}
// 离开该场景时调用；持有期间不要提前释放。
sceneHandle.Dispose();
```

`SceneHandle.Result` 是成功标记；Handle 内部记录实际 Scene。`Dispose()` 发起该场景的异步卸载，返回不代表卸载完成。同名场景的并发加载会被拒绝。

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

预加载会对规范化后的路径去重，提供 `TotalCount`、`LoadedCount`、`FailedCount` 和 `FailedPaths`。它持有批量资源引用，不直接提供资源对象；释放它会释放内部 Handle。

## 释放与迁移

- 每个加载请求持有自己的 Handle，重复 `Dispose()` 不会重复释放该 Handle 的引用。
- `UIUtils.ReplaceSpriteHandle(...)` 等工具返回的 Handle 也由调用者保管，在图片不再使用时释放；不要忽略返回值。
- 加载期间取消或释放 Handle，不保证底层 Unity 加载立即停止；迟到的加载结果由 Handle 清理。用 `try/finally` 或 `using` 保证退出时释放。
- Bundle 中无引用资源可进入 Retention 缓存，保留时间由配置控制；仍被引用的资源不会因缓存清理而主动失去所有权。
- `UnloadUnusedAssets(maxDepth: 5)` 最多执行指定轮数的缓存清理，以处理依赖释放；它不是精确的依赖图层数限制。实现还调用 Unity 的未使用资源清理和 GC，不宜逐帧执行。
- 旧的 AssetSystem 回调加载、非 Handle 加载、按路径/对象释放、手动场景卸载接口已移除。实例化改用 `InstantiateHandle`，批量预加载改用 `PreloadAssetHandle<T>`。
- 原先的 `var result = await handle.Task` 改为 `await handle; var result = handle.Result;`，并保持 Handle 覆盖资源使用期。不要通过 `GetAwaiter().GetResult()` 同步读取尚未完成的加载；将调用链改为异步等待。

战斗模块在 `LiteNexusDataManager.Startup()` 中等待配置加载，再读取数据库；运行时同步访问 `LiteNexusConfig.Instance` 前必须完成该初始化，初始化失败或关闭数据管理器时释放配置 Handle。编辑器工具仍使用 AssetDatabase 访问配置。

完整签名见 [AssetSystem.cs](AssetSystem.cs)，所有权实现见 [Handle](Handle)。
