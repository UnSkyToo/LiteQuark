using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace LiteQuark.Runtime
{
    public sealed class HttpClient
    {
        private readonly NetworkSystem _networkSystem;
        private readonly List<IHttpInterceptor> _interceptors = new List<IHttpInterceptor>();

        internal HttpClient(NetworkSystem networkSystem)
        {
            _networkSystem = networkSystem;
        }

        #region Interceptor Management

        public void AddInterceptor(IHttpInterceptor interceptor)
        {
            if (interceptor != null && !_interceptors.Contains(interceptor))
            {
                _interceptors.Add(interceptor);
            }
        }

        public void RemoveInterceptor(IHttpInterceptor interceptor)
        {
            _interceptors.Remove(interceptor);
        }

        public void ClearInterceptors()
        {
            _interceptors.Clear();
        }

        #endregion

        #region Basic HTTP Methods

        public async UniTask<HttpResponse<T>> Get<T>(string url, Dictionary<string, string> headers = null, int timeout = 0)
        {
            var request = new HttpRequest(url, HttpMethod.GET)
            {
                Headers = headers ?? new Dictionary<string, string>(),
                Timeout = timeout > 0 ? timeout : _networkSystem.DefaultTimeout
            };
            return await Send<T>(request);
        }

        public async UniTask<HttpResponse<T>> PostJson<T>(string url, object data, Dictionary<string, string> headers = null, int timeout = 0)
        {
            var json = LitJson.JsonMapper.ToJson(data);
            var request = new HttpRequest(url, HttpMethod.POST)
            {
                Headers = headers ?? new Dictionary<string, string>(),
                Timeout = timeout > 0 ? timeout : _networkSystem.DefaultTimeout
            };
            request.SetJsonBody(json);
            return await Send<T>(request);
        }

        public async UniTask<HttpResponse<T>> Put<T>(string url, object data, Dictionary<string, string> headers = null, int timeout = 0)
        {
            var json = LitJson.JsonMapper.ToJson(data);
            var request = new HttpRequest(url, HttpMethod.PUT)
            {
                Headers = headers ?? new Dictionary<string, string>(),
                Timeout = timeout > 0 ? timeout : _networkSystem.DefaultTimeout
            };
            request.SetJsonBody(json);
            return await Send<T>(request);
        }

        public async UniTask<HttpResponse<T>> Delete<T>(string url, Dictionary<string, string> headers = null, int timeout = 0)
        {
            var request = new HttpRequest(url, HttpMethod.DELETE)
            {
                Headers = headers ?? new Dictionary<string, string>(),
                Timeout = timeout > 0 ? timeout : _networkSystem.DefaultTimeout
            };
            return await Send<T>(request);
        }

        #endregion

        #region File Upload/Download

        public async UniTask<byte[]> DownloadFile(string url, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            await _networkSystem.WaitForSlot();

            try
            {
                using var request = UnityWebRequest.Get(url);
                request.timeout = _networkSystem.DefaultTimeout;

                var operation = request.SendWebRequest();

                while (!operation.isDone && !cancellationToken.IsCancellationRequested)
                {
                    progress?.Report(operation.progress);
                    await UniTask.Yield();
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    request.Abort();
                    throw new OperationCanceledException("Download cancelled");
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    progress?.Report(1.0f);
                    return request.downloadHandler.data;
                }

                throw new Exception($"Download failed: {request.error}");
            }
            finally
            {
                _networkSystem.ReleaseSlot();
            }
        }

        public async UniTask<HttpResponse<T>> UploadFile<T>(string url, byte[] fileData, string fileName, string fieldName = "file", Dictionary<string, string> headers = null, IProgress<float> progress = null, int timeout = 0)
        {
            await _networkSystem.WaitForSlot();

            try
            {
                var form = new List<IMultipartFormSection>
                {
                    new MultipartFormFileSection(fieldName, fileData, fileName, "application/octet-stream")
                };

                using var request = UnityWebRequest.Post(url, form);
                request.timeout = timeout > 0 ? timeout : _networkSystem.DefaultTimeout;

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.SetRequestHeader(header.Key, header.Value);
                    }
                }

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    progress?.Report(operation.progress);
                    await UniTask.Yield();
                }

                progress?.Report(1.0f);

                var rawResponse = CreateResponseFromWebRequest(request);
                return HttpResponse<T>.FromRawResponse(rawResponse);
            }
            finally
            {
                _networkSystem.ReleaseSlot();
            }
        }

        #endregion

        #region Retry Mechanism

        public async UniTask<HttpResponse<T>> SendWithRetry<T>(HttpRequest request, HttpRetryPolicy retryPolicy = null)
        {
            if (retryPolicy == null)
            {
                if (_networkSystem.EnableAutoRetry)
                {
                    retryPolicy = new HttpRetryPolicy(_networkSystem.DefaultRetryCount);
                }
                else
                {
                    return await Send<T>(request);
                }
            }

            var attempt = 0;
            while (attempt <= retryPolicy.MaxRetries)
            {
                var response = await Send<T>(request);

                if (response.IsSuccess)
                {
                    return response;
                }

                var rawResponse = new HttpResponse(response.IsSuccess, response.StatusCode, null, response.Error);
                if (!retryPolicy.ShouldRetry(rawResponse))
                {
                    return response;
                }

                if (attempt < retryPolicy.MaxRetries)
                {
                    var delay = retryPolicy.GetDelayForAttempt(attempt);
                    LLog.Warning("Request failed, retrying in {0}s... (Attempt {1}/{2})", delay, attempt + 1, retryPolicy.MaxRetries);
                    await UniTask.Delay(TimeSpan.FromSeconds(delay));
                }

                attempt++;
            }

            return new HttpResponse<T>(false, 0, default, null, $"Request failed after {retryPolicy.MaxRetries} retries");
        }

        #endregion

        #region Batch Requests

        public async UniTask<HttpResponse<T>[]> SendBatch<T>(HttpRequest[] requests, int maxConcurrent = 0)
        {
            if (requests == null || requests.Length == 0)
            {
                return Array.Empty<HttpResponse<T>>();
            }

            var concurrency = maxConcurrent > 0 ? maxConcurrent : _networkSystem.MaxConcurrentRequests;
            var results = new HttpResponse<T>[requests.Length];
            var tasks = new List<UniTask>();

            for (var i = 0; i < requests.Length; i++)
            {
                var index = i;
                tasks.Add(SendAndAssign(requests[index], results, index));

                if (tasks.Count >= concurrency)
                {
                    await UniTask.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }

            return results;
        }
        
        private async UniTask SendAndAssign<T>(HttpRequest request, HttpResponse<T>[] results, int index)
        {
            results[index] = await Send<T>(request);
        }

        #endregion

        #region Core Send Method

        public async UniTask<HttpResponse<T>> Send<T>(HttpRequest request)
        {
            var rawResponse = await SendRaw(request);
            return HttpResponse<T>.FromRawResponse(rawResponse);
        }

        public async UniTask<HttpResponse> SendRaw(HttpRequest request)
        {
            await _networkSystem.WaitForSlot();

            try
            {
                // Apply interceptors - OnRequest
                foreach (var interceptor in _interceptors)
                {
                    request = await interceptor.OnRequest(request);
                }

                HttpResponse response;

                using (var webRequest = CreateUnityWebRequest(request))
                {
                    var operation = webRequest.SendWebRequest();

                    while (!operation.isDone && !request.CancellationToken.IsCancellationRequested)
                    {
                        await UniTask.Yield();
                    }

                    if (request.CancellationToken.IsCancellationRequested)
                    {
                        webRequest.Abort();
                        return new HttpResponse(false, 0, null, "Request cancelled");
                    }

                    response = CreateResponseFromWebRequest(webRequest);
                }

                // Apply interceptors - OnResponse
                foreach (var interceptor in _interceptors)
                {
                    response = await interceptor.OnResponse(response);
                }

                return response;
            }
            catch (Exception ex)
            {
                LLog.Error($"HTTP request error: {ex.Message}");
                return new HttpResponse(false, 0, null, ex.Message);
            }
            finally
            {
                _networkSystem.ReleaseSlot();
            }
        }

        #endregion

        #region Helper Methods

        private UnityWebRequest CreateUnityWebRequest(HttpRequest request)
        {
            UnityWebRequest webRequest;

            switch (request.Method)
            {
                case HttpMethod.GET:
                    webRequest = UnityWebRequest.Get(request.Url);
                    break;
                case HttpMethod.POST:
                    webRequest = new UnityWebRequest(request.Url, "POST");
                    if (request.Body != null)
                    {
                        webRequest.uploadHandler = new UploadHandlerRaw(request.Body);
                    }
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    break;
                case HttpMethod.PUT:
                    webRequest = UnityWebRequest.Put(request.Url, request.Body ?? Array.Empty<byte>());
                    break;
                case HttpMethod.DELETE:
                    webRequest = UnityWebRequest.Delete(request.Url);
                    break;
                default:
                    throw new NotSupportedException($"HTTP method {request.Method} is not supported");
            }

            webRequest.timeout = request.Timeout;

            foreach (var header in request.Headers)
            {
                webRequest.SetRequestHeader(header.Key, header.Value);
            }

            return webRequest;
        }

        private HttpResponse CreateResponseFromWebRequest(UnityWebRequest webRequest)
        {
            var isSuccess = webRequest.result == UnityWebRequest.Result.Success;
            var statusCode = webRequest.responseCode;
            var data = isSuccess ? webRequest.downloadHandler.data : null;
            var error = isSuccess ? null : webRequest.error;

            return new HttpResponse(isSuccess, statusCode, data, error);
        }

        #endregion
    }
}
