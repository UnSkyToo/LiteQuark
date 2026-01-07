# LiteQuark Framework

**LiteQuark** 是一个轻量级、高性能的 Unity 游戏开发框架，提供了完整的系统架构和开箱即用的核心功能模块。

## 核心特性

### 架构设计
- **System/Logic 分离**：清晰的系统层和业务逻辑层分离
- **生命周期管理**：统一的 Stage/Pipeline 生命周期（Boot → InitSystem → InitLogic → Main）
- **服务定位模式**：通过 `LiteRuntime.Get<T>()` 访问所有系统服务
- **零配置启动**：系统自动注册和初始化

### 核心功能
- **事件驱动架构**：类型安全的事件系统，支持全局和独立模块
- **资源管理**：AssetBundle 管理，Retention 缓存策略
- **异步编程**：基于 UniTask 的现代异步模式
- **网络通讯**：完整的 HTTP 客户端，支持文件上传/下载
- **数据持久化**：多种存储方式（PlayerPrefs/JSON/Binary），支持加密
- **对象池**：GameObject 和集合对象池，减少 GC 压力
- **UI 管理**：完整的 UI 生命周期和层级管理
- **音频管理**：音效/音乐分类管理，音量控制

---

## 快速开始

### 1. 创建引导场景

```csharp
using LiteQuark.Runtime;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        // 框架会自动初始化所有系统
        DontDestroyOnLoad(gameObject);
    }
}
```

### 2. 访问系统服务

```csharp
// 事件系统
LiteRuntime.Event.Emit(new PlayerLevelUpEvent { Level = 10 });

// 资源系统
var prefab = await LiteRuntime.Asset.LoadAsync<GameObject>("Prefabs/Player");

// 网络系统
var response = await LiteRuntime.Http.GetAsync("https://api.example.com/data");

// 数据系统
LiteRuntime.Data.Save("playerName", "Alice");

// 定时器
LiteRuntime.Timer.AddTimer(1.0f, () => Debug.Log("1秒后执行"));

// 对象池
var bullet = LiteRuntime.ObjectPool.Get("Bullet", "Prefabs/Bullet");
```

---

## 系统模块

### 核心系统

| 系统 | 说明 | 文档 |
|-----|------|------|
| **EventSystem** | 类型安全的事件系统，支持全局和独立模块 | [README](Runtime/Core/Event/README.md) |
| **AssetSystem** | 资源加载管理，支持 AssetBundle 和 Retention 缓存 | [README](Runtime/Asset/README.md) |
| **NetworkSystem** | HTTP 网络通讯，支持文件上传/下载和重试机制 | [README](Runtime/Network/README.md) |
| **DataSystem** | 数据持久化，支持多种存储方式和加密 | [README](Runtime/Data/README.md) |
| **TaskSystem** | 异步任务管理，协程和并发控制 | [README](Runtime/Core/Async/Task/README.md) |
| **TimerSystem** | 定时器系统，支持时间和帧定时器 | [README](Runtime/Core/Async/Timer/README.md) |
| **ActionSystem** | 动画和缓动系统，支持链式调用 | [README](Runtime/Core/Async/Action/README.md) |
| **ObjectPoolSystem** | 对象池系统，GameObject 和集合复用 | [README](Runtime/Core/ObjectPool/README.md) |

### 扩展模块 (Addons)

| 模块 | 说明 | 文档 |
|-----|------|------|
| **UISystem** | UI 生命周期管理，Canvas 层级管理，安全区适配 | [Addons/UI/README.md](../LiteQuark.Addons/Runtime/UI/README.md) |
| **AudioSystem** | 音频管理系统，音效/音乐分类，音量控制 | [Addons/Audio/README.md](../LiteQuark.Addons/Runtime/Audio/README.md) |

---

## 框架架构

### 生命周期阶段

```
Boot (引导阶段)
  ↓
InitSystem (系统初始化)
  ↓
InitLogic (业务逻辑初始化)
  ↓
Main (主循环)
```

### 系统优先级

系统按照优先级顺序初始化（数字越小越早初始化）：

```csharp
EventSystem     = 99900  // 事件系统（最先）
ObjectPool      = 99800  // 对象池
Asset           = 99700  // 资源管理
Network         = 99500  // 网络通讯
Timer           = 99400  // 定时器
Task            = 99300  // 任务系统
Action          = 99200  // 动画系统
Data            = 99100  // 数据持久化
```

### 目录结构

```
Assets/LiteQuark/
├── Runtime/
│   ├── Base/               # 基础类（LiteRuntime, LiteSetting, LiteConst）
│   ├── Core/
│   │   ├── Event/          # 事件系统
│   │   ├── Async/
│   │   │   ├── Task/       # 任务系统
│   │   │   ├── Timer/      # 定时器系统
│   │   │   └── Action/     # 动画系统
│   │   └── ObjectPool/     # 对象池系统
│   ├── Asset/              # 资源系统
│   ├── Network/            # 网络系统
│   └── Data/               # 数据系统
│
Assets/LiteQuark.Addons/
├── Runtime/
│   ├── UI/                 # UI 系统
│   └── Audio/              # 音频系统
```

---

## 配置系统

框架通过 `LiteSetting` 类进行配置：

```csharp
// 网络配置
LiteSetting.Network.MaxConcurrentRequests = 10;
LiteSetting.Network.DefaultTimeout = 30;
LiteSetting.Network.EnableAutoRetry = true;

// 数据持久化配置
LiteSetting.Data.ProviderMode = DataProviderMode.JsonFile;
LiteSetting.Data.EnableEncryption = true;
LiteSetting.Data.EncryptionKey = "your-secret-key";

// 任务系统配置
LiteSetting.Task.ConcurrencyLimit = 20;
LiteSetting.Task.IgnoreLimitPriority = TaskPriority.High;

// 资源配置
LiteSetting.Asset.DefaultCacheStrategy = CacheStrategy.Retention;
```

---

## 最佳实践

### 1. 事件驱动设计

使用事件系统解耦模块：

```csharp
// 定义事件
public struct CoinChangedEvent : IEventData
{
    public int TotalCoins;
}

// 发送事件
LiteRuntime.Event.Emit(new CoinChangedEvent { TotalCoins = 100 });

// 监听事件
LiteRuntime.Event.Register<CoinChangedEvent>(OnCoinChanged);

void OnCoinChanged(CoinChangedEvent evt)
{
    UpdateCoinDisplay(evt.TotalCoins);
}
```

### 2. 异步资源加载

```csharp
// 异步加载资源
var prefab = await LiteRuntime.Asset.LoadAsync<GameObject>("Prefabs/Player");
var player = Instantiate(prefab);

// 批量加载
var textures = await LiteRuntime.Asset.LoadAllAsync<Texture2D>("Textures/UI");
```

### 3. 使用对象池

```csharp
// 获取对象
var bullet = LiteRuntime.ObjectPool.Get("Bullet", "Prefabs/Bullet");
bullet.SetActive(true);

// 回收对象
LiteRuntime.ObjectPool.Release("Bullet", bullet);
```

### 4. 定时器管理

```csharp
// 保存定时器 ID 以便停止
private ulong timerId;

void StartTimer()
{
    timerId = LiteRuntime.Timer.AddTimer(1.0f, OnTick, TimerSystem.RepeatCountForever);
}

void StopTimer()
{
    LiteRuntime.Timer.StopTimer(timerId);
}

void OnDestroy()
{
    // 销毁时清理定时器
    if (timerId != 0)
    {
        LiteRuntime.Timer.StopTimer(timerId);
    }
}
```

### 5. HTTP 网络请求

```csharp
// GET 请求
var response = await LiteRuntime.Http.GetAsync("https://api.example.com/user/123");
if (response.IsSuccess)
{
    var userData = LitJson.JsonMapper.ToObject<UserData>(response.Text);
}

// POST 请求
var postData = new { username = "player1", score = 1000 };
var response = await LiteRuntime.Http.PostAsync(
    "https://api.example.com/score",
    LitJson.JsonMapper.ToJson(postData)
);

// 文件下载（带进度）
await LiteRuntime.Http.DownloadFileAsync(
    "https://cdn.example.com/update.zip",
    "C:/Downloads/update.zip",
    progress => Debug.Log($"下载进度：{progress * 100}%")
);
```

### 6. 数据持久化

```csharp
// 保存数据
LiteRuntime.Data.Save("playerLevel", 10);
LiteRuntime.Data.Save("playerName", "Alice");

// 读取数据
int level = LiteRuntime.Data.Load<int>("playerLevel", defaultValue: 1);
string name = LiteRuntime.Data.Load<string>("playerName", "Guest");

// 异步保存复杂对象
var playerData = new PlayerData { Level = 10, Coins = 500 };
await LiteRuntime.Data.SaveAsync("playerData", playerData);

// 异步加载
var data = await LiteRuntime.Data.LoadAsync<PlayerData>("playerData");
```

---

## 性能优化

### 1. 使用对象池减少 GC

```csharp
// 频繁创建的对象使用对象池
var enemies = LiteRuntime.ObjectPool.GetList<Enemy>();
// ... 使用 ...
LiteRuntime.ObjectPool.ReleaseList(enemies); // 自动清空
```

### 2. 资源卸载

```csharp
// 及时卸载不用的资源
LiteRuntime.Asset.UnloadAsync("Prefabs/LargeScene");

// 场景切换时清理
LiteRuntime.Asset.UnloadAll();
```

### 3. 控制并发请求数

```csharp
// 限制同时加载的资源数量
LiteSetting.Asset.MaxConcurrentLoads = 5;

// 限制网络并发请求
LiteSetting.Network.MaxConcurrentRequests = 10;
```

### 4. 分帧加载

```csharp
IEnumerator LoadResourcesInBatches(string[] paths)
{
    foreach (var path in paths)
    {
        var resource = await LiteRuntime.Asset.LoadAsync<GameObject>(path);
        ProcessResource(resource);

        // 每加载一个资源后等待一帧
        yield return null;
    }
}
```

---

## 依赖项

- **Unity 2022.3+**
- **UniTask** - 异步编程库
- **LitJson** - JSON 序列化

---

## 扩展开发

### 创建自定义系统

```csharp
using LiteQuark.Runtime;

public class CustomSystem : BaseSystem
{
    public override int Priority => 99000; // 设置优先级

    protected override void OnInit()
    {
        // 系统初始化
        Debug.Log("CustomSystem initialized");
    }

    protected override void OnUpdate(float deltaTime)
    {
        // 每帧更新
    }

    protected override void OnDestroy()
    {
        // 清理资源
    }
}
```

### 注册自定义系统

```csharp
// 在 LiteConst.cs 中注册
public static class LiteConst
{
    public static readonly Dictionary<Type, int> SystemPriority = new()
    {
        // ... 其他系统 ...
        { typeof(CustomSystem), 99000 }
    };
}
```

---

## 示例项目

查看 `Assets/Scripts/` 目录下的示例代码：

- **NetworkSystemExample.cs** - 网络系统使用示例
- **DataSystemExample.cs** - 数据持久化示例
- **DemoLogic.cs** - 完整的游戏逻辑示例

---

## 常见问题

### Q: 如何在非 MonoBehaviour 类中使用框架？

A: 通过 `LiteRuntime` 静态类访问所有系统服务：

```csharp
public class GameManager // 非 MonoBehaviour
{
    public void Initialize()
    {
        LiteRuntime.Event.Emit(new GameStartEvent());
        var config = await LiteRuntime.Asset.LoadAsync<GameConfig>("Config/game");
    }
}
```

### Q: 系统初始化顺序如何控制？

A: 通过 `Priority` 属性控制，数字越小越早初始化。参考 `LiteConst.SystemPriority`。

### Q: 如何处理场景切换？

A: 使用 TaskSystem 的协程功能：

```csharp
LiteRuntime.Task.AddTask(LoadSceneCoroutine("NewScene"));

IEnumerator LoadSceneCoroutine(string sceneName)
{
    // 显示加载界面
    ShowLoadingScreen();

    // 卸载旧场景资源
    yield return LiteRuntime.Asset.UnloadAllAsync();

    // 加载新场景
    var asyncOp = SceneManager.LoadSceneAsync(sceneName);
    yield return asyncOp;

    // 隐藏加载界面
    HideLoadingScreen();
}
```

### Q: 如何处理网络请求失败？

A: 使用内置的重试机制：

```csharp
LiteSetting.Network.EnableAutoRetry = true;
LiteSetting.Network.DefaultRetryCount = 3;

var response = await LiteRuntime.Http.GetAsync("https://api.example.com/data");
if (!response.IsSuccess)
{
    Debug.LogError($"请求失败：{response.Error}");
}
```

---

## 许可证

MIT License

Copyright (c) 2026 LiteQuark Framework

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---

## 联系方式

- **Issues**: 在 GitHub 提交问题反馈
- **文档**: 查看各模块的 README 文档

---

**LiteQuark** - 轻量、高效、易用的 Unity 游戏框架
