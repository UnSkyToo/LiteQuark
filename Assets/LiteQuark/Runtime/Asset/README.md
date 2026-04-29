# AssetSystem 资源管理系统

AssetSystem 是 LiteQuark 的资源加载与生命周期管理模块。对外推荐使用 Handle API：加载得到 Handle，资源通过 `handle.Result` 访问，释放统一调用 `Dispose()`。

## 核心原则

- `LoadAssetHandle<T>` / `InstantiateHandle` / `LoadSceneHandle` 是主 API。
- `await handle` 只表示等待加载完成，不返回资源本体。
- 持有资源就持有 Handle；释放资源就释放 Handle。
- 对外不再提供 `LoadAssetAsync<T>`、`InstantiateAsync`、callback 加载入口、path/object 手动释放入口或手动场景卸载入口。

## 加载模式

| 模式 | 适用场景 | 说明 |
| --- | --- | --- |
| Editor | 编辑器开发 | 直接从 Assets 加载，便于迭代 |
| Bundle | 生产环境 | AssetBundle 加载，支持内置包和远程包 |
| Resource | 小型项目 | 使用 Resources 目录加载 |

在 `LiteSetting.Asset` 中配置 `AssetMode`、`BundleLocater`、远程地址、并发数和 Retention 时间。

## 推荐 API

### 加载资源

```csharp
using var handle = LiteRuntime.Asset.LoadAssetHandle<Texture2D>("UI/Icon");
await handle;

var icon = handle.Result;
```

需要跨作用域持有资源时，把 Handle 保存到拥有者字段，并在 `Dispose` / `OnDestroy` / 关闭界面时释放。

```csharp
private AssetHandle<GameObject> _prefabHandle;

public async UniTask Load()
{
    _prefabHandle = LiteRuntime.Asset.LoadAssetHandle<GameObject>("Prefabs/Player");
    await _prefabHandle;
    var prefab = _prefabHandle.Result;
}

public void Dispose()
{
    _prefabHandle?.Dispose();
    _prefabHandle = null;
}
```

### 实例化 GameObject

```csharp
var handle = LiteRuntime.Asset.InstantiateHandle("Prefabs/Enemy", parent);
await handle;

var enemy = handle.Result;

// 不再需要实例时：
handle.Dispose();
```

`GameObjectHandle.Dispose()` 会销毁实例，并释放内部持有的 prefab Handle。

### 加载场景

```csharp
var parameters = new LoadSceneParameters(LoadSceneMode.Additive);
var handle = LiteRuntime.Asset.LoadSceneHandle("Scenes/Battle", parameters);
await handle;

if (!handle.Result)
{
    LLog.Error("load scene failed");
}

// 离开场景时：
handle.Dispose();
```

`SceneHandle.Dispose()` 会卸载该 Handle 持有的场景。

## 公共 API 边界

当前对外保留的资源生命周期 API：

```csharp
LiteRuntime.Asset.LoadAssetHandle<T>(path);
LiteRuntime.Asset.PreloadAssetHandle<T>(paths);
LiteRuntime.Asset.InstantiateHandle(path, parent);
LiteRuntime.Asset.LoadSceneHandle(path, parameters);
LiteRuntime.Asset.UnloadUnusedAssets(maxDepth);
```

内部 provider 仍使用 callback 适配底层加载流程，但 callback 不再作为 public API 暴露。引用释放只能通过 `Dispose()`，path 级接口只保留在 Handle 内部用于释放一次引用。

## 预加载

批量预加载使用 `PreloadAssetHandle<T>`。它只负责加载并持有一组资源引用，不暴露资源本体；需要使用资源时仍通过 `LoadAssetHandle<T>` 获取自己的 Handle。

```csharp
var paths = new[] { "UI/IconA", "UI/IconB" };
using var preload = LiteRuntime.Asset.PreloadAssetHandle<Sprite>(paths);
await preload;

if (!preload.Result)
{
    foreach (var path in preload.FailedPaths)
    {
        LLog.Error("preload failed: {0}", path);
    }
}
```

`PreloadHandle<T>.Dispose()` 会释放所有已持有的内部资源引用。同批次重复路径会被去重。

## Retention 缓存

资源释放后会进入 Retention 缓存，在配置的保留时间内再次加载可以复用缓存。可在场景切换或内存压力较高时主动调用：

```csharp
LiteRuntime.Asset.UnloadUnusedAssets(maxDepth: 5);
```

`maxDepth` 控制依赖资源递归释放深度，例如 `A -> B -> C`，传入 `2` 时最多释放到 `B`。

## 实践建议

- UI、音频、对象池等拥有长期资源的对象应保存 Handle 字段。
- 临时读取配置或数据文件时使用 `using var handle`。
- 静态工具如果会加载资源，应返回 Handle；例如 `UIUtils.ReplaceSpriteHandle(...)`。
- 不要把 `handle.Result` 存成唯一生命周期依据；资源所有权在 Handle 上。
- 新增资源、脚本或目录时提交对应 `.meta` 文件。
