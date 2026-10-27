using Cysharp.Threading.Tasks;

namespace LiteBattle.Runtime
{
    public interface IManager
    {
        UniTask<bool> Startup();
        void Shutdown();
    }
}
