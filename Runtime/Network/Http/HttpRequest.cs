using System.Collections.Generic;
using System.Threading;

namespace LiteQuark.Runtime
{
    public sealed class HttpRequest
    {
        public string Url { get; set; }
        public HttpMethod Method { get; set; } = HttpMethod.GET;
        public Dictionary<string, string> Headers { get; set; }
        public byte[] Body { get; set; }
        public int Timeout { get; set; } = 10;
        public CancellationToken CancellationToken { get; set; }

        public HttpRequest()
        {
            Headers = new Dictionary<string, string>();
        }

        public HttpRequest(string url, HttpMethod method = HttpMethod.GET)
        {
            Url = url;
            Method = method;
            Headers = new Dictionary<string, string>();
        }

        public void AddHeader(string key, string value)
        {
            Headers[key] = value;
        }

        public void SetJsonBody(string json)
        {
            Body = System.Text.Encoding.UTF8.GetBytes(json);
            AddHeader("Content-Type", "application/json");
        }
    }
}
