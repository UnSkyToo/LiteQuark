using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public interface IStartup
    {
        UniTask<bool> Startup();
    }
}