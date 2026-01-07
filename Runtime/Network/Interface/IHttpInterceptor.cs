using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// 网络拦截器接口，用于在请求前后进行统一处理
    /// 例如：添加签名、加密、日志记录等
    /// </summary>
    public interface IHttpInterceptor
    {
        UniTask<HttpRequest> OnRequest(HttpRequest request);
        UniTask<HttpResponse> OnResponse(HttpResponse response);
    }
}
