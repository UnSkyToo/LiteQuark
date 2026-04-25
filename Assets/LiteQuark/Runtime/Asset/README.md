# AssetSystem 资源管理系统

AssetSystem 是 LiteQuark 框架的资源加载和管理模块，提供了统一的资源加载接口和多种加载模式。

## 功能特性

### ✅ 核心功能
- **三种加载模式**：Editor模式（开发）/ Bundle模式（生产）/ Resource模式
- **场景管理**：通过 `SceneHandle` 管理场景加载和卸载
- **资源预加载**：通过持有 `AssetHandle` 预热资源
- **Retention缓存**：自动管理资源生命周期，减少重复加载
- **远程Bundle**：支持从服务器下载AssetBundle

### ✅ 设计特点
- **模式切换**：一键切换加载模式，无需修改代码
- **Handle优先**：资源、实例和场景都通过 Handle 表达所有权
- **并发控制**：限制同时加载资源数量
- **自动卸载**：Retention机制自动管理资源释放

---

## 快速开始

### 1. 配置资源设置

在 LiteSetting 中配置：

```csharp
资源设置:
├── 资源模式: Editor / Bundle / Resource
├── Bundle定位器: BuiltIn / Remote
├── 远程资源根目录: https://cdn.example.com/
├── 并发加载数量限制: 5
├── 开启资源缓存模式: ✓
├── 资源保留时间: 120秒
└── Bundle保留时间: 300秒
```

### 2. 加载资源

```csharp
// 加载预制体，Handle.Dispose 会释放这次资源引用
var prefabHandle = LiteRuntime.Asset.LoadAssetHandle<GameObject>("Prefabs/Player");
var prefab = await prefabHandle;

// 实例化，GameObjectHandle.Dispose 会销毁实例并释放Prefab引用
var instanceHandle = LiteRuntime.Asset.InstantiateHandle("Prefabs/Enemy", transform);
var instance = await instanceHandle;

// 加载场景
var sceneHandle = LiteRuntime.Asset.LoadSceneHandle(
    "Scenes/Battle",
    new LoadSceneParameters(LoadSceneMode.Additive));
await sceneHandle;
```

---

## 加载模式对比

| 模式 | 适用场景 | 特点 |
|------|---------|------|
| **Editor** | 开发调试 | 直接从Assets目录加载，快速迭代 |
| **Bundle** | 生产环境 | AssetBundle加载，支持热更新 |
| **Resource** | 小型项目 | 使用Resources目录，无需打包 |

---

## API 文档

### 资源加载

#### LoadAssetHandle<T>
```csharp
public AssetHandle<T> LoadAssetHandle<T>(string path, CancellationToken ct = default) where T : Object
```

**参数：**
- `path` - 资源路径（相对于Assets目录）
- `ct` - 取消令牌

**示例：**
```csharp
// 加载预制体
var handle = LiteRuntime.Asset.LoadAssetHandle<GameObject>("Prefabs/Player");
var prefab = await handle;
handle.Dispose();

// 加载材质
var matHandle = LiteRuntime.Asset.LoadAssetHandle<Material>("Materials/PlayerMat");
renderer.material = await matHandle;
```

#### InstantiateHandle
```csharp
public GameObjectHandle InstantiateHandle(string path, Transform parent, CancellationToken ct = default)
```

一步到位：加载预制体并实例化。

**示例：**
```csharp
var handle = LiteRuntime.Asset.InstantiateHandle("Prefabs/Bullet", firePoint);
var bullet = await handle;
bullet.GetComponent<Bullet>().Fire(direction);
handle.Dispose();
```

---

### 场景加载

#### LoadSceneHandle
```csharp
public SceneHandle LoadSceneHandle(string path, LoadSceneParameters parameters, CancellationToken ct = default)
```

**示例：**
```csharp
// 加载关卡场景
var handle = LiteRuntime.Asset.LoadSceneHandle(
    "Scenes/Level01",
    new LoadSceneParameters(LoadSceneMode.Single));
await handle;

// 释放句柄会卸载由该句柄加载的场景
handle.Dispose();
```

---

### 资源预加载

预加载资源时保存 `AssetHandle`。需要释放预热引用时 Dispose 对应 Handle。

**示例：**
```csharp
var playerHandle = LiteRuntime.Asset.LoadAssetHandle<GameObject>("Prefabs/Player");
var enemyHandle = LiteRuntime.Asset.LoadAssetHandle<GameObject>("Prefabs/Enemy");

await playerHandle;
await enemyHandle;
```

---

### 资源卸载

资源卸载由 Handle 所有权驱动。`AssetHandle.Dispose()` 释放资源引用，`GameObjectHandle.Dispose()` 销毁实例并释放 Prefab 引用，`SceneHandle.Dispose()` 卸载场景。

---

## 使用场景

### 1. 动态加载UI

```csharp
public class UIManager
{
    public async UniTask OpenShop()
    {
        var handle = LiteRuntime.Asset.InstantiateHandle("UI/ShopPanel", canvas.transform);
        var panel = await handle;
        var shopUI = panel.GetComponent<ShopUI>();
        shopUI.Initialize();

        // UI关闭时由UI系统或调用方 Dispose handle
    }
}
```

### 2. 关卡资源管理

```csharp
public class LevelManager
{
    public async UniTask LoadLevel(int levelId)
    {
        var enemies = LiteRuntime.Asset.LoadAssetHandle<GameObject>($"Prefabs/Level{levelId}/Enemies");
        var props = LiteRuntime.Asset.LoadAssetHandle<GameObject>($"Prefabs/Level{levelId}/Props");
        var skybox = LiteRuntime.Asset.LoadAssetHandle<Texture>($"Textures/Level{levelId}/Skybox");

        await enemies;
        await props;
        await skybox;

        var scene = LiteRuntime.Asset.LoadSceneHandle(
            $"Scenes/Level{levelId}",
            new LoadSceneParameters(LoadSceneMode.Single));
        await scene;

        Debug.Log("关卡加载完成");
    }
}
```

### 3. Retention缓存机制

```csharp
// 系统会自动管理资源生命周期
// 卸载的资源会保留在缓存中一段时间（默认120秒）
// 如果在此期间再次加载，直接从缓存返回（无需重新加载）

// 第一次加载
var handle = LiteRuntime.Asset.LoadAssetHandle<GameObject>("Prefabs/Boss");
var boss = await handle;

// 使用完毕后卸载
handle.Dispose();

// 120秒内再次加载 -> 直接从缓存返回（极快）
var nextHandle = LiteRuntime.Asset.LoadAssetHandle<GameObject>("Prefabs/Boss");
var nextBoss = await nextHandle;
```

---

## Bundle模式配置

### 1. Bundle打包（Editor）

使用Unity的AssetBundle打包工具或第三方工具打包资源。

### 2. 配置远程地址

```csharp
LiteSetting.Asset:
├── 资源模式: Bundle
├── Bundle定位器: Remote
└── 远程资源根目录: https://cdn.example.com/bundles/
```

### 3. 版本管理

Bundle模式会自动从远程下载版本文件（version.txt），并根据版本号下载更新的Bundle。

---

## 配置说明

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| AssetMode | Enum | Editor | 资源加载模式 |
| BundleLocater | Enum | BuiltIn | Bundle定位器（包内/远程） |
| BundleRemoteUri | string | - | 远程资源根目录 |
| ConcurrencyLimit | int | 5 | 并发加载数量限制 |
| BundleDownloadRetry | RetryParam | - | 下载重试参数 |
| EnableRetain | bool | true | 是否启用Retention缓存 |
| AssetRetainTime | float | 120 | 资源保留时间（秒） |
| BundleRetainTime | float | 300 | Bundle保留时间（秒） |

---

## 最佳实践

### 1. 使用 Handle 加载

```csharp
var handle = LiteRuntime.Asset.LoadAssetHandle<GameObject>("Prefabs/Player");
var prefab = await handle;

handle.Dispose();
```

### 2. 预加载关键资源

```csharp
async UniTask Start()
{
    _playerHandle = LiteRuntime.Asset.LoadAssetHandle<GameObject>("Prefabs/Player");
    _hudHandle = LiteRuntime.Asset.LoadAssetHandle<GameObject>("UI/HUD");

    await _playerHandle;
    await _hudHandle;
}
```

### 3. 及时卸载不用的资源

```csharp
public class LevelCleanup
{
    private readonly List<IDispose> _handles = new();

    public void OnLevelEnd()
    {
        foreach (var handle in _handles)
        {
            handle.Dispose();
        }
        _handles.Clear();

        // 系统会自动进入Retention缓存
        // 如果后续不再使用，会在保留时间后自动释放
    }
}
```

---
