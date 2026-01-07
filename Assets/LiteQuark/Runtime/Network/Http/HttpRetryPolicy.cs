using System;

namespace LiteQuark.Runtime
{
    public sealed class HttpRetryPolicy
    {
        public int MaxRetries { get; set; } = 3;
        public float RetryDelay { get; set; } = 1.0f;
        public bool ExponentialBackoff { get; set; } = false;
        public Func<HttpResponse, bool> ShouldRetry { get; set; }

        public HttpRetryPolicy()
        {
            ShouldRetry = DefaultShouldRetry;
        }

        public HttpRetryPolicy(int maxRetries, float retryDelay = 1.0f, bool exponentialBackoff = false)
        {
            MaxRetries = maxRetries;
            RetryDelay = retryDelay;
            ExponentialBackoff = exponentialBackoff;
            ShouldRetry = DefaultShouldRetry;
        }

        private static bool DefaultShouldRetry(HttpResponse response)
        {
            // Retry on server errors (5xx) or network errors
            return !response.IsSuccess && (response.StatusCode >= 500 || response.StatusCode == 0 || response.Error.Contains("timeout") || response.Error.Contains("Unknown Error") || response.Error.Contains("connection"));
        }

        public float GetDelayForAttempt(int attempt)
        {
            if (!ExponentialBackoff)
            {
                return RetryDelay;
            }

            return RetryDelay * UnityEngine.Mathf.Pow(2, attempt);
        }
    }
}
