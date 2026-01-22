using System;
using Cysharp.Threading.Tasks;

namespace LiteQuark.Runtime
{
    /// <summary>
    /// 网络通道接口，用于扩展TCP/WebSocket等自定义通道
    /// 业务层可以实现此接口来提供自定义的网络通信
    /// </summary>
    public interface INetworkChannel : IDispose
    {
        bool IsConnected { get; }

        UniTask<bool> Connect(string host, int port);
        void Send(byte[] data);
        void Close();

        event Action OnConnected;
        event Action OnDisconnected;
        event Action<byte[] > OnReceived;
        event Action<string> OnError;
    }
}
