# NetworkSystem 网络模块

NetworkSystem 是 LiteQuark 框架的网络通信模块，提供了简单易用的 HTTP 请求功能。

## 功能特性

### ✅ 核心功能
- **基础HTTP方法**：GET / POST / PUT / DELETE
- **文件操作**：上传/下载文件，支持进度回调
- **并发控制**：限制同时请求数量，避免服务器压力
- **自动重试**：请求失败自动重试，支持指数退避
- **批量请求**：一次发送多个请求
- **拦截器**：统一处理请求/响应（添加Token、日志等）

### ✅ 设计特点
- **轻量化**：只封装 UnityWebRequest，无第三方依赖
- **易用性**：一行代码完成请求
- **扩展性**：提供接口支持 TCP/WebSocket 等自定义通道

---

## 快速开始

### 1. 配置网络设置

在 LiteSetting 中配置网络参数：

```csharp
网络设置:
├── 最大并发请求数: 5
├── 默认超时时间: 10秒
├── 启用自动重试: true
└── 默认重试次数: 3
```

### 2. 发送简单请求

```csharp
// GET 请求
var response = await LiteRuntime.Http.Get<UserData>("https://api.example.com/user/123");
if (response.IsSuccess)
{
    Debug.Log($"用户名：{response.Data.name}");
}

// POST JSON
var loginData = new LoginRequest { username = "player1", password = "123456" };
var result = await LiteRuntime.Http.PostJson<LoginResponse>("https://api.example.com/login", loginData);
```

---

## API 文档

### 基础请求方法

#### Get<T>
```csharp
public async UniTask<HttpResponse<T>> Get<T>(
    string url,
    Dictionary<string, string> headers = null,
    int timeout = 0)
```

#### PostJson<T>
```csharp
public async UniTask<HttpResponse<T>> PostJson<T>(
    string url,
    object data,
    Dictionary<string, string> headers = null,
    int timeout = 0)
```

#### Put<T> / Delete<T>
```csharp
public async UniTask<HttpResponse<T>> Put<T>(string url, object data, ...);
public async UniTask<HttpResponse<T>> Delete<T>(string url, ...);
```

---

### 文件上传/下载

#### DownloadFile
```csharp
public async UniTask<byte[]> DownloadFile(
    string url,
    IProgress<float> progress = null,
    CancellationToken cancellationToken = default)
```

**示例：**
```csharp
var progress = new Progress<float>(p => Debug.Log($"下载进度：{p * 100}%"));
var fileData = await LiteRuntime.Http.DownloadFile("https://cdn.example.com/asset.bundle", progress);
```

#### UploadFile
```csharp
public async UniTask<HttpResponse<T>> UploadFile<T>(
    string url,
    byte[] fileData,
    string fileName,
    string fieldName = "file",
    Dictionary<string, string> headers = null,
    IProgress<float> progress = null,
    int timeout = 0)
```

**示例：**
```csharp
var screenshot = File.ReadAllBytes("screenshot.png");
var response = await LiteRuntime.Http.UploadFile<UploadResponse>(
    "https://api.example.com/upload",
    screenshot,
    "screenshot.png",
    progress: new Progress<float>(p => Debug.Log($"上传进度：{p * 100}%"))
);
```

---

### 高级功能

#### 请求重试
```csharp
var retryPolicy = new RetryPolicy
{
    MaxRetries = 3,
    RetryDelay = 1.0f,
    ExponentialBackoff = true  // 1s, 2s, 4s
};

var response = await LiteRuntime.Http.SendWithRetry<ConfigData>(request, retryPolicy);
```

#### 批量请求
```csharp
var requests = new[]
{
    new HttpRequest("https://api.example.com/user/1", HttpMethod.GET),
    new HttpRequest("https://api.example.com/user/2", HttpMethod.GET),
};

var responses = await LiteRuntime.Http.SendBatch<UserData>(requests, maxConcurrent: 3);
```

#### 拦截器
```csharp
public class AuthInterceptor : IHttpInterceptor
{
    public async UniTask<HttpRequest> OnRequest(HttpRequest request)
    {
        request.AddHeader("Authorization", $"Bearer {GetToken()}");
        return request;
    }

    public async UniTask<HttpResponse> OnResponse(HttpResponse response)
    {
        LLog.Info($"响应状态码：{response.StatusCode}");
        return response;
    }
}

// 添加拦截器
LiteRuntime.Http.AddInterceptor(new AuthInterceptor());
```

---

## 扩展接口

### 自定义网络通道（TCP/WebSocket）

框架提供了 `INetworkChannel` 接口，业务层可以实现自定义的网络通道：

```csharp
public class MyWebSocketChannel : INetworkChannel
{
    private WebSocket ws;

    public bool IsConnected => ws?.IsAlive ?? false;

    public async UniTask<bool> Connect(string host, int port)
    {
        ws = new WebSocket($"ws://{host}:{port}");
        ws.OnMessage += (sender, e) => OnReceived?.Invoke(e.RawData);
        ws.OnError += (sender, e) => OnError?.Invoke(e.Message);
        await ws.ConnectAsync();
        return ws.IsAlive;
    }

    public void Send(byte[] data) => ws.Send(data);
    public void Close() => ws.Close();
    public void Dispose() => ws?.Close();

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<byte[]> OnReceived;
    public event Action<string> OnError;
}
```

### 自定义协议编解码（Protobuf/MessagePack）

```csharp
public class ProtobufCodec : IProtocolCodec
{
    public byte[] Encode<T>(T message)
    {
        using var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, message);
        return stream.ToArray();
    }

    public T Decode<T>(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return ProtoBuf.Serializer.Deserialize<T>(stream);
    }
}
```

---

## 配置说明

### LiteSetting 配置项

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| MaxConcurrentRequests | int | 5 | 最大并发请求数 |
| DefaultTimeout | int | 10 | 默认超时时间（秒） |
| EnableAutoRetry | bool | true | 启用自动重试 |
| DefaultRetryCount | int | 3 | 默认重试次数 |

---

## 常见问题

### Q: 如何处理认证Token？
**A:** 使用拦截器统一添加：
```csharp
LiteRuntime.Http.AddInterceptor(new AuthInterceptor());
```

### Q: 如何取消正在进行的请求？
**A:** 使用 CancellationToken：
```csharp
var cts = new CancellationTokenSource();
var request = new HttpRequest { Url = "...", CancellationToken = cts.Token };
// 取消请求
cts.Cancel();
```

### Q: 如何处理大文件上传？
**A:** 使用 UploadFile 方法，它会自动分块上传并报告进度。

### Q: 是否支持 HTTPS？
**A:** 是的，UnityWebRequest 默认支持 HTTPS。

### Q: 如何实现 WebSocket 通信？
**A:** 实现 INetworkChannel 接口，参考扩展接口章节。

---

## 最佳实践

### 1. 使用重试机制提高稳定性
```csharp
// 对于重要的配置请求，启用重试
var response = await LiteRuntime.Http.SendWithRetry<ConfigData>(request);
```

### 2. 控制并发数量
```csharp
// 在 LiteSetting 中配置合理的 MaxConcurrentRequests
// 避免同时发送过多请求导致服务器拒绝服务
```

### 3. 使用拦截器统一处理
```csharp
// 统一添加Token、记录日志、错误处理
LiteRuntime.Http.AddInterceptor(new LoggingInterceptor());
```

### 4. 异常处理
```csharp
try
{
    var response = await LiteRuntime.Http.Get<UserData>(url);
    if (!response.IsSuccess)
    {
        LLog.Error($"请求失败：{response.Error}");
    }
}
catch (Exception ex)
{
    LLog.Error($"网络异常：{ex.Message}");
}
```

---

## 性能建议

1. **复用HttpClient实例**：框架内部自动管理，无需手动创建
2. **合理设置超时时间**：根据网络环境调整 DefaultTimeout
3. **使用批量请求**：减少单个请求的开销
4. **避免主线程阻塞**：使用 async/await，不要用 .Wait() 或 .Result

---

