namespace LiteQuark.Runtime
{
    public static class IDGenerator
    {
        private static ulong ID_ = 1;

        public static ulong NextID()
        {
            return ID_++;
        }
    }
}