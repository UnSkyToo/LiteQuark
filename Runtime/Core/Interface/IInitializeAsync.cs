using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public interface IInitializeAsync
    {
        Task<bool> Initialize();
    }
}