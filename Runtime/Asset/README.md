# AssetSystem 资源管理系统

AssetSystem 是 LiteQuark 框架的资源加载和管理模块，提供了统一的资源加载接口和多种加载模式。

## 功能特性

### ✅ 核心功能
- **三种加载模式**：Editor模式（开发）/ Bundle模式（生产）/ Resource模式
- **场景管理**：支持同步/异步场景加载
- **资源预加载**：批量预加载资源
- **Retention缓存**：自动管理资源生命周期，减少重复加载
- **远程Bundle**：支持从服务器下载AssetBundle

### ✅ 设计特点
- **模式切换**：一键切换加载模式，无需修改代码
- **异步优先**：基于UniTask的异步加载
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
// 异步加载预制体
LiteRuntime.Asset.LoadAssetAsync<GameObject>("Prefabs/Player", prefab =>
{
    var instance = Instantiate(prefab);
});

// 异步实例化（自动加载+实例化）
LiteRuntime.Asset.InstantiateAsync("Prefabs/Enemy", transform, instance =>
{
    // 使用实例化的GameObject
});

// 加载场景
LiteRuntime.Asset.LoadSceneAsync("Scenes/Battle", "Battle", UnityEngine.SceneManagement.LoadSceneMode.Additive, progress =>
{
    Debug.Log($"加载进度：{progress}%");
});
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

#### LoadAssetAsync<T>
```csharp
public void LoadAssetAsync<T>(string path, Action<T> callback) where T : Object
```

**参数：**
- `path` - 资源路径（相对于Assets目录）
- `callback` - 加载完成回调

**示例：**
```csharp
// 加载预制体
LiteRuntime.Asset.LoadAssetAsync<GameObject>("Prefabs/Player", prefab =>
{
    var player = Instantiate(prefab);
});

// 加载材质
LiteRuntime.Asset.LoadAssetAsync<Material>("Materials/PlayerMat", mat =>
{
    renderer.material = mat;
});
```

#### InstantiateAsync
```csharp
public void InstantiateAsync(string path, Transform parent, Action<GameObject> callback)
```

一步到位：加载预制体并实例化。

**示例：**
```csharp
LiteRuntime.Asset.InstantiateAsync("Prefabs/Bullet", firePoint, bullet =>
{
    bullet.GetComponent<Bullet>().Fire(direction);
});
```

---

### 场景加载

#### LoadSceneAsync
```csharp
public void LoadSceneAsync(string path, string sceneName, LoadSceneMode mode, Action<float> progressCallback = null)
```

**示例：**
```csharp
// 加载关卡场景
LiteRuntime.Asset.LoadSceneAsync(
    "Scenes/Level01",
    "Level01",
    LoadSceneMode.Single,
    progress => loadingBar.value = progress
);
```

---

### 资源预加载

#### PreloadAssets
```csharp
public void PreloadAssets(string[] paths, Action onComplete = null)
```

批量预加载资源。

**示例：**
```csharp
string[] assetsToPreload = new[]
{
    "Prefabs/Player",
    "Prefabs/Enemy",
    "UI/MainMenu"
};

LiteRuntime.Asset.PreloadAssets(assetsToPreload, () =>
{
    Debug.Log("预加载完成");
    StartGame();
});
```

---

### 资源卸载

#### UnloadAsset
```csharp
public void UnloadAsset(string path)
```

卸载指定资源（进入Retention缓存）。

**示例：**
```csharp
LiteRuntime.Asset.UnloadAsset("Prefabs/OldEnemy");
```

---

## 使用场景

### 1. 动态加载UI

```csharp
public class UIManager
{
    public void OpenShop()
    {
        LiteRuntime.Asset.InstantiateAsync("UI/ShopPanel", canvas.transform, panel =>
        {
            var shopUI = panel.GetComponent<ShopUI>();
            shopUI.Initialize();
        });
    }
}
```

### 2. 关卡资源管理

```csharp
public class LevelManager
{
    public async UniTask LoadLevel(int levelId)
    {
        // 预加载关卡资源
        string[] levelAssets = new[]
        {
            $"Prefabs/Level{levelId}/Enemies",
            $"Prefabs/Level{levelId}/Props",
            $"Textures/Level{levelId}/Skybox"
        };

        await PreloadAssetsAsync(levelAssets);

        // 加载场景
        await LoadSceneAsync($"Scenes/Level{levelId}", $"Level{levelId}");

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
LiteRuntime.Asset.LoadAssetAsync<GameObject>("Prefabs/Boss", boss1 => { });

// 使用完毕后卸载
LiteRuntime.Asset.UnloadAsset("Prefabs/Boss");

// 120秒内再次加载 -> 直接从缓存返回（极快）
LiteRuntime.Asset.LoadAssetAsync<GameObject>("Prefabs/Boss", boss2 => { });
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
| BundleDownloadTimeout | int | 60 | 下载超时时间（秒） |
| BundleDownloadMaxRetries | int | 3 | 下载重试次数 |
| EnableRetain | bool | true | 是否启用Retention缓存 |
| AssetRetainTime | float | 120 | 资源保留时间（秒） |
| BundleRetainTime | float | 300 | Bundle保留时间（秒） |

---

## 最佳实践

### 1. 使用异步加载

```csharp
// ✅ 推荐：异步加载（不阻塞主线程）
LiteRuntime.Asset.LoadAssetAsync<GameObject>("Prefabs/Player", prefab => { });

// ❌ 避免：同步加载（阻塞主线程）
```

### 2. 预加载关键资源

```csharp
void Start()
{
    // 在loading界面预加载游戏关键资源
    string[] criticalAssets = new[]
    {
        "Prefabs/Player",
        "Prefabs/MainCamera",
        "UI/HUD"
    };

    LiteRuntime.Asset.PreloadAssets(criticalAssets, OnPreloadComplete);
}
```

### 3. 及时卸载不用的资源

```csharp
public class LevelCleanup
{
    public void OnLevelEnd()
    {
        // 卸载关卡特定资源
        LiteRuntime.Asset.UnloadAsset("Prefabs/Level01/Boss");
        LiteRuntime.Asset.UnloadAsset("Prefabs/Level01/Props");

        // 系统会自动进入Retention缓存
        // 如果后续不再使用，会在保留时间后自动释放
    }
}
```

---
