# TaskSystem 异步任务系统

TaskSystem 是 LiteQuark 框架的异步任务管理模块，提供了统一的异步操作接口和并发控制。

## 功能特性

### ✅ 核心功能
- **协程任务**：执行Unity协程（IEnumerator）
- **AsyncOperation任务**：包装Unity异步操作
- **并发控制**：限制同时运行的任务数量
- **优先级调度**：高优先级任务可跳过并发限制
- **主线程同步**：在主线程执行回调

### ✅ 设计特点
- **统一接口**：所有异步操作统一管理
- **自动调度**：根据优先级和并发限制自动调度
- **状态管理**：Pending → InProgress → Done
- **异常处理**：自动捕获并记录异常

---

## 快速开始

### 1. 协程任务

```csharp
// 执行协程
LiteRuntime.Task.AddTask(MyCoroutine(), () =>
{
    Debug.Log("协程完成");
});

IEnumerator MyCoroutine()
{
    Debug.Log("开始");
    yield return new WaitForSeconds(2.0f);
    Debug.Log("2秒后");
}
```

### 2. AsyncOperation任务

```csharp
// 包装Unity异步操作
var asyncOp = SceneManager.LoadSceneAsync("NewScene");
LiteRuntime.Task.AddTask(asyncOp, () =>
{
    Debug.Log("场景加载完成");
});
```

### 3. 自定义回调任务

```csharp
// 异步操作（如网络请求）
LiteRuntime.Task.AddTask(callback =>
{
    // 异步操作
    StartNetworkRequest(result =>
    {
        // 完成时调用callback
        callback(result.success);
    });
});
```

---

## 任务优先级

```csharp
public enum TaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2
}
```

**并发控制规则：**
- 默认并发限制：20个任务
- **High优先级**：忽略并发限制，立即执行
- **Normal/Low优先级**：等待空闲槽位

**配置：**
```csharp
LiteSetting.Task:
├── 并发任务数量限制: 20
└── 忽略并发限制起始等级: High
```

---

## API 文档

### AddTask (协程)
```csharp
public CoroutineTask AddTask(IEnumerator taskFunc, Action callback = null)
```

执行Unity协程。

**示例：**
```csharp
var task = LiteRuntime.Task.AddTask(LoadData(), OnLoadComplete);

IEnumerator LoadData()
{
    yield return new WaitForSeconds(1.0f);
    // 加载数据...
}

void OnLoadComplete()
{
    Debug.Log("数据加载完成");
}
```

### AddTask (AsyncOperation)
```csharp
public AsyncOperationTask AddTask(AsyncOperation asyncOperation, Action callback = null)
```

包装Unity异步操作。

**示例：**
```csharp
// 场景加载
var asyncOp = SceneManager.LoadSceneAsync("Level01");
LiteRuntime.Task.AddTask(asyncOp, () => Debug.Log("场景加载完成"));

// 资源加载
var request = Resources.LoadAsync<GameObject>("Prefabs/Player");
LiteRuntime.Task.AddTask(request, () =>
{
    var prefab = request.asset as GameObject;
    Instantiate(prefab);
});
```

### AddTask (回调)
```csharp
public WaitCallbackTask AddTask(Action<Action<bool>> func)
```

执行自定义异步操作。

**示例：**
```csharp
// 网络请求
LiteRuntime.Task.AddTask(callback =>
{
    HttpClient.Get("https://api.example.com/data", response =>
    {
        ProcessData(response);
        callback(true); // 成功
    });
});
```

### AddMainThreadTask
```csharp
public MainThreadTask AddMainThreadTask(Action<object> func, object param)
```

在主线程执行任务。

**示例：**
```csharp
// 从子线程切回主线程
Task.Run(() =>
{
    // 子线程工作
    var data = ProcessHeavyWork();

    // 切回主线程更新UI
    LiteRuntime.Task.AddMainThreadTask(obj =>
    {
        UpdateUI((DataType)obj);
    }, data);
});
```

---

## 主线程同步

### PostMainThreadTask
```csharp
public void PostMainThreadTask(SendOrPostCallback callback, object state)
```

异步投递到主线程（不阻塞）。

**示例：**
```csharp
// 子线程
new Thread(() =>
{
    var result = CalculateResult();

    // 投递到主线程
    LiteRuntime.Task.PostMainThreadTask(state =>
    {
        Debug.Log($"结果：{state}");
    }, result);
}).Start();
```

### SendMainThreadTask
```csharp
public void SendMainThreadTask(SendOrPostCallback callback, object state)
```

同步发送到主线程（阻塞直到完成）。

---

## 使用场景

### 1. 资源分帧加载

```csharp
public class ResourceLoader
{
    public void LoadMultipleResources(string[] paths, Action onComplete)
    {
        LiteRuntime.Task.AddTask(LoadCoroutine(paths), onComplete);
    }

    IEnumerator LoadCoroutine(string[] paths)
    {
        foreach (var path in paths)
        {
            var request = Resources.LoadAsync<GameObject>(path);
            yield return request;

            var prefab = request.asset as GameObject;
            RegisterPrefab(path, prefab);

            // 每加载一个资源后等待一帧（避免卡顿）
            yield return null;
        }
    }
}
```

### 2. 场景过渡

```csharp
public class SceneTransition
{
    public void LoadScene(string sceneName)
    {
        LiteRuntime.Task.AddTask(LoadSceneCoroutine(sceneName));
    }

    IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // 显示加载界面
        ShowLoadingScreen();

        yield return new WaitForSeconds(0.5f);

        // 卸载旧场景
        var unloadOp = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        LiteRuntime.Task.AddTask(unloadOp);
        yield return unloadOp;

        // 加载新场景
        var loadOp = SceneManager.LoadSceneAsync(sceneName);
        LiteRuntime.Task.AddTask(loadOp, () =>
        {
            HideLoadingScreen();
        });

        // 显示加载进度
        while (!loadOp.isDone)
        {
            UpdateLoadingProgress(loadOp.progress);
            yield return null;
        }
    }
}
```

### 3. 异步网络请求

```csharp
public class NetworkManager
{
    public void FetchServerData()
    {
        LiteRuntime.Task.AddTask(callback =>
        {
            // 发起网络请求
            HttpRequest("https://api.example.com/config", response =>
            {
                if (response.success)
                {
                    ParseConfig(response.data);
                    callback(true);
                }
                else
                {
                    Debug.LogError("请求失败");
                    callback(false);
                }
            });
        });
    }
}
```

---

## 任务状态监控

```csharp
public class TaskMonitor : MonoBehaviour
{
    void Update()
    {
        // 监控任务队列状态
        int running = LiteRuntime.Task.RunningTaskCount;
        int pending = LiteRuntime.Task.PendingTaskCount;

        Debug.Log($"运行中：{running}, 等待中：{pending}");

        // 如果队列过长，可以考虑优化
        if (pending > 50)
        {
            Debug.LogWarning("任务队列过长！");
        }
    }
}
```

---

## 并发控制

### 配置并发限制

```csharp
// 在LiteSetting中配置
LiteSetting.Task:
├── ConcurrencyLimit: 20        // 最多同时运行20个任务
└── IgnoreLimitPriority: High   // High优先级忽略限制
```

### 优先级示例

```csharp
// 高优先级任务（立即执行）
var highTask = new CustomTask { Priority = TaskPriority.High };
LiteRuntime.Task.AddTask(highTask);

// 普通优先级任务（等待槽位）
var normalTask = new CustomTask { Priority = TaskPriority.Normal };
LiteRuntime.Task.AddTask(normalTask);
```

---

## 最佳实践

### 1. 合理设置并发限制

```csharp
// 根据设备性能调整
#if UNITY_ANDROID || UNITY_IOS
    LiteSetting.Task.ConcurrencyLimit = 10; // 移动端保守
#else
    LiteSetting.Task.ConcurrencyLimit = 20; // PC端激进
#endif
```

### 2. 避免长时间阻塞

```csharp
// ❌ 不好：长时间计算
IEnumerator BadCoroutine()
{
    for (int i = 0; i < 1000000; i++)
    {
        HeavyCalculation(); // 阻塞帧
    }
    yield break;
}

// ✅ 好：分帧处理
IEnumerator GoodCoroutine()
{
    for (int i = 0; i < 1000000; i++)
    {
        HeavyCalculation();

        if (i % 100 == 0)
        {
            yield return null; // 每100次等待一帧
        }
    }
}
```

### 3. 使用高优先级处理关键任务

```csharp
// 玩家输入响应（高优先级）
var inputTask = new InputHandlerTask { Priority = TaskPriority.High };
LiteRuntime.Task.AddTask(inputTask);

// 背景资源加载（低优先级）
var loadTask = new BackgroundLoadTask { Priority = TaskPriority.Low };
LiteRuntime.Task.AddTask(loadTask);
```

---

## 性能建议

### 1. 监控任务队列

定期检查 `RunningTaskCount` 和 `PendingTaskCount`，避免任务堆积。

### 2. 分批加载

```csharp
// 批量加载，每批之间等待
IEnumerator LoadInBatches(string[] paths, int batchSize)
{
    for (int i = 0; i < paths.Length; i += batchSize)
    {
        for (int j = 0; j < batchSize && i + j < paths.Length; j++)
        {
            LoadResource(paths[i + j]);
        }
        yield return null; // 每批后等待一帧
    }
}
```

---

