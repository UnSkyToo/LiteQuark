using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public interface IInitializeAsync
    {
        UniTask<bool> Initialize();
    }
}