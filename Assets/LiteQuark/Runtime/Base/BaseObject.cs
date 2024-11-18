namespace LiteQuark.Runtime
{
    public abstract class BaseObject : IUniqueID
    {
        public ulong UniqueID { get; }
        public abstract string DebugName { get; }

        protected BaseObject()
        {
            UniqueID = IDGenerator.NextID();
        }

        public sealed override string ToString() => DebugName;
    }
}