namespace LiteQuark.Runtime
{
    /// <summary>
    /// 协议编解码器接口，用于扩展Protobuf/MessagePack等序列化协议
    /// 业务层可以实现此接口来提供自定义的序列化/反序列化逻辑
    /// </summary>
    public interface IProtocolCodec
    {
        byte[] Encode<T>(T message);
        T Decode<T>(byte[] data);
    }
}
