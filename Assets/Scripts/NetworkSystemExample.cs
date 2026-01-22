using System;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

/// <summary>
/// NetworkSystem使用示例
/// 演示了HTTP请求的各种用法
/// </summary>
public class NetworkSystemExample : MonoBehaviour
{
    [Serializable]
    public class UserData
    {
        public int id;
        public string name;
        public string email;
    }

    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class LoginResponse
    {
        public bool success;
        public string token;
        public string message;
    }

    // ============================================
    // 1. 简单GET请求
    // ============================================
    public async void Example_SimpleGet()
    {
        var response = await LiteRuntime.Get<NetworkSystem>().Http.Get<UserData>("https://jsonplaceholder.typicode.com/users/1");

        if (response.IsSuccess)
        {
            Debug.Log($"用户名：{response.Data.name}");
            Debug.Log($"邮箱：{response.Data.email}");
        }
        else
        {
            Debug.LogError($"请求失败：{response.Error}");
        }
    }

    // ============================================
    // 2. POST JSON请求
    // ============================================
    public async void Example_PostJson()
    {
        var loginData = new LoginRequest
        {
            username = "player1",
            password = "123456"
        };

        var response = await LiteRuntime.Get<NetworkSystem>().Http.PostJson<LoginResponse>(
            "https://example.com/api/login",
            loginData
        );

        if (response.IsSuccess)
        {
            Debug.Log($"登录成功，Token: {response.Data.token}");
        }
        else
        {
            Debug.LogError($"登录失败：{response.Error}");
        }
    }

    // ============================================
    // 3. 文件下载（带进度）
    // ============================================
    public async void Example_DownloadFile()
    {
        var progress = new Progress<float>(p =>
        {
            Debug.Log($"下载进度：{p * 100:F1}%");
        });

        try
        {
            var fileData = await LiteRuntime.Get<NetworkSystem>().Http.DownloadFile(
                "https://example.com/asset.bundle",
                progress
            );

            Debug.Log($"下载完成，文件大小：{fileData.Length} bytes");
            // 保存文件或处理数据
        }
        catch (Exception ex)
        {
            Debug.LogError($"下载失败：{ex.Message}");
        }
    }

    // ============================================
    // 4. 文件上传（带进度）
    // ============================================
    public async void Example_UploadFile()
    {
        // 假设有一个截图
        var screenshot = System.IO.File.ReadAllBytes("screenshot.png");

        var progress = new Progress<float>(p =>
        {
            Debug.Log($"上传进度：{p * 100:F1}%");
        });

        var response = await LiteRuntime.Get<NetworkSystem>().Http.UploadFile<LoginResponse>(
            "https://example.com/api/upload",
            screenshot,
            "screenshot.png",
            "file",
            progress: progress
        );

        if (response.IsSuccess)
        {
            Debug.Log("上传成功");
        }
    }

    // ============================================
    // 5. 带重试的请求
    // ============================================
    public async void Example_RequestWithRetry()
    {
        var request = new HttpRequest
        {
            Url = "https://example.com/api/config",
            Method = HttpMethod.GET
        };

        var retryPolicy = new HttpRetryPolicy
        {
            MaxRetries = 3,
            RetryDelay = 1.0f,
            ExponentialBackoff = true // 1s, 2s, 4s
        };

        var response = await LiteRuntime.Get<NetworkSystem>().Http.SendWithRetry<UserData>(request, retryPolicy);

        if (response.IsSuccess)
        {
            Debug.Log("请求成功");
        }
        else
        {
            Debug.LogError($"重试{retryPolicy.MaxRetries}次后仍然失败");
        }
    }

    // ============================================
    // 6. 批量请求
    // ============================================
    public async void Example_BatchRequests()
    {
        var requests = new[]
        {
            new HttpRequest("https://jsonplaceholder.typicode.com/users/1", HttpMethod.GET),
            new HttpRequest("https://jsonplaceholder.typicode.com/users/2", HttpMethod.GET),
            new HttpRequest("https://jsonplaceholder.typicode.com/users/3", HttpMethod.GET),
        };

        var responses = await LiteRuntime.Get<NetworkSystem>().Http.SendBatch<UserData>(requests, maxConcurrent: 2);

        for (int i = 0; i < responses.Length; i++)
        {
            if (responses[i].IsSuccess)
            {
                Debug.Log($"用户{i + 1}：{responses[i].Data.name}");
            }
        }
    }

    // ============================================
    // 7. 自定义请求头
    // ============================================
    public async void Example_CustomHeaders()
    {
        var headers = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Authorization", "Bearer YOUR_TOKEN" },
            { "X-Custom-Header", "CustomValue" }
        };

        var response = await LiteRuntime.Get<NetworkSystem>().Http.Get<UserData>(
            "https://example.com/api/user",
            headers
        );

        if (response.IsSuccess)
        {
            Debug.Log("请求成功");
        }
    }

    // ============================================
    // 8. 使用拦截器（统一处理）
    // ============================================
    public class AuthInterceptor : IHttpInterceptor
    {
        public async UniTask<HttpRequest> OnRequest(HttpRequest request)
        {
            // 在请求前添加认证Token
            request.AddHeader("Authorization", "Bearer YOUR_TOKEN");
            Debug.Log($"发送请求：{request.Url}");
            return request;
        }

        public async UniTask<HttpResponse> OnResponse(HttpResponse response)
        {
            // 在响应后记录日志
            Debug.Log($"收到响应，状态码：{response.StatusCode}");
            return response;
        }
    }

    public void Example_UseInterceptor()
    {
        // 添加拦截器
        LiteRuntime.Get<NetworkSystem>().Http.AddInterceptor(new AuthInterceptor());

        // 之后所有请求都会自动添加Token和记录日志
    }

    // ============================================
    // 9. 检查网络系统状态
    // ============================================
    public void Example_CheckNetworkStatus()
    {
        var networkSystem = LiteRuntime.Get<NetworkSystem>();
        Debug.Log($"最大并发数：{networkSystem.MaxConcurrentRequests}");
        Debug.Log($"当前活动请求数：{networkSystem.ActiveRequests}");
        Debug.Log($"默认超时时间：{networkSystem.DefaultTimeout}秒");
    }
}
