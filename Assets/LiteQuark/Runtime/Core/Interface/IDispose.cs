using System;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public interface IDispose : IDisposable
    {
    }

    public interface IDisposeAsync
    {
        UniTask DisposeAsync();
    }
}
