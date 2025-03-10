using System.Threading.Tasks;

namespace LiteQuark.Runtime
{
    public interface IStartup
    {
        Task<bool> Startup();
    }
}