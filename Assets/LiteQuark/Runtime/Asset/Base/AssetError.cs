using System;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// Immutable information about the resource operation that failed.
    /// </summary>
    public sealed class AssetError
    {
        public string ResourceKey { get; }
        public string Message { get; }
        public Exception Exception { get; }

        public AssetError(string resourceKey, string message, Exception exception = null)
        {
            ResourceKey = resourceKey;
            Message = message;
            Exception = exception;
        }
    }
}
