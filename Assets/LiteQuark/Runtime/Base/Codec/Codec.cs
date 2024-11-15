namespace LiteQuark.Runtime
{
    public static class Codec
    {
        public static byte[] Encode<T>(T value)
        {
            return new Encoder().Encode(value);
        }
        
        public static T Decode<T>(byte[] data)
        {
            return new Decoder().Decode<T>(data);
        }
    }
}