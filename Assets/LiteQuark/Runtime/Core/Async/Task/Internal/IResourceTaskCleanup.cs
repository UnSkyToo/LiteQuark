using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    internal interface IResourceTaskCleanup
    {
        UniTask WaitForCleanupAsync();
    }
}
