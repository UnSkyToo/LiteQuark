using System;

namespace LiteQuark.Runtime
{
    public sealed class HttpResponse
    {
        public bool IsSuccess { get; set; }
        public long StatusCode { get; set; }
        public string Error { get; set; }
        public byte[] Data { get; set; }
        public string Text => Data != null ? System.Text.Encoding.UTF8.GetString(Data) : string.Empty;

        public HttpResponse(bool isSuccess, long statusCode, byte[] data, string error = null)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            Data = data;
            Error = error;
        }
    }

    public sealed class HttpResponse<T>
    {
        public bool IsSuccess { get; set; }
        public long StatusCode { get; set; }
        public string Error { get; set; }
        public T Data { get; set; }
        public string RawText { get; set; }

        public HttpResponse(bool isSuccess, long statusCode, T data, string rawText = null, string error = null)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            Data = data;
            RawText = rawText;
            Error = error;
        }

        public static HttpResponse<T> FromRawResponse(HttpResponse rawResponse)
        {
            if (!rawResponse.IsSuccess)
            {
                return new HttpResponse<T>(false, rawResponse.StatusCode, default, null, rawResponse.Error);
            }

            try
            {
                var text = rawResponse.Text;
                var data = UnityEngine.JsonUtility.FromJson<T>(text);
                return new HttpResponse<T>(true, rawResponse.StatusCode, data, text);
            }
            catch (Exception ex)
            {
                return new HttpResponse<T>(false, rawResponse.StatusCode, default, rawResponse.Text, $"JSON parse error: {ex.Message}");
            }
        }
    }
}
