using System;

namespace LiteQuark.Runtime
{
    public interface IInitializeAsync
    {
        void Initialize(Action<bool> callback);
    }
}