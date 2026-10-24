using System;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// One loading outcome. The payload keeps its existing ownership and release rules.
    /// </summary>
    internal sealed class AssetLoadResult<T> where T : class
    {
        public bool Succeeded => Error == null;
        public T Value { get; }
        public AssetError Error { get; }

        private AssetLoadResult(T value, AssetError error)
        {
            Value = value;
            Error = error;
        }

        public static AssetLoadResult<T> Success(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return new AssetLoadResult<T>(value, null);
        }

        public static AssetLoadResult<T> Failure(AssetError error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            return new AssetLoadResult<T>(null, error);
        }
    }
}
