# NetworkSystem HTTP

NetworkSystem 属于 LiteQuark.Addons。将它加入 Launcher 的 `SystemList`，并在 `SystemSettings` 中配置 `NetworkSystemSetting`。通过 `LiteRuntime.Get<NetworkSystem>().Http` 获取客户端。

## 请求示例

以下方法在框架初始化后、Unity 主线程上调用：

```csharp
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;

public static class HttpExample
{
    public static async UniTask<string> FetchText(string url)
    {
        var network = LiteRuntime.Get<NetworkSystem>();
        var request = new HttpRequest(url)
        {
            Timeout = network.DefaultTimeout
        };
        var response = await network.Http.SendRaw(request);
        if (!response.IsSuccess)
        {
            LLog.Warning("HTTP {0}: {1}", response.StatusCode, response.Error);
            return null;
        }
        return response.Text;
    }
}
```

| API | 结果 / 用途 |
| --- | --- |
| `Get<T>`、`PostJson<T>`、`Put<T>`、`Delete<T>` | `HttpResponse<T>`；读取 `IsSuccess`、`StatusCode`、`Data`、`RawText`、`Error` |
| `Send<T>(HttpRequest)` | 自定义请求并解析 JSON |
| `SendRaw(HttpRequest)` | `HttpResponse`，保留字节 `Data` 和 `Text` |
| `SendWithRetry<T>(request, policy)` | 显式启用重试流程 |
| `SendBatch<T>(requests, maxConcurrent)` | 分组并发，结果顺序与输入一致；仍是多个独立请求 |
| `DownloadFile(url, progress, cancellationToken)` | 下载到内存并返回 `byte[]`，不直接保存文件 |
| `UploadFile<T>(url, fileData, fileName, ...)` | 用完整 `byte[]` 创建 multipart 请求，不是分块上传 |

POST/PUT 的对象通过 LitJson 编码；泛型响应通过 `UnityEngine.JsonUtility` 解码，因此响应类型必须符合 JsonUtility 的字段/结构约束。需要原始字符串或自定义解析时使用 `SendRaw`。

## 配置与重试

`NetworkSystemSetting` 默认并发 5、超时 10 秒、`EnableAutoRetry = true`、重试次数 3。保持并发值大于 0。

`Get` / `PostJson` / `Put` / `Delete` 和 `SendBatch` 不会自动调用重试。只有 `SendWithRetry` 在未传 policy 时读取上述重试配置。手动传入 `HttpRetryPolicy` 可设置 `MaxRetries`、`RetryDelay`、`ExponentialBackoff`、`ShouldRetry`；默认不是指数退避。写入类请求重试前应由业务处理幂等性。

`HttpRequest.Timeout` 自身默认 10 秒。手动创建请求时，若要使用系统当前超时，应显式赋值；基础便捷方法会采用系统默认值。

## 取消、进度与拦截器

- 自定义请求通过 `HttpRequest.CancellationToken` 取消；`SendRaw` 将取消作为失败响应返回。基础便捷方法没有直接的 token 参数。
- `DownloadFile` 支持 token，取消时抛出取消异常，失败时抛出异常；`UploadFile` 当前没有 token 参数。
- 等待并发槽位的过程没有 token 检查，取消不保证在排队期间立即生效。
- 上传/下载进度参数是 `IProgress<float>`，可传入 `new System.Progress<float>(value => ...)`。
- `AddInterceptor` / `RemoveInterceptor` / `ClearInterceptors` 管理 `IHttpInterceptor`。普通请求经过 `OnRequest` 和 `OnResponse`；独立上传、下载方法不经过这条拦截器链。

`INetworkChannel`、`IProtocolCodec` 是扩展接口；仓库没有内置 TCP/WebSocket 通道实现。详见 [HttpClient.cs](Http/HttpClient.cs)、[请求](Http/HttpRequest.cs) 和 [响应](Http/HttpResponse.cs)。
