namespace LiteQuark.Runtime
{
    public abstract class ObjectBase
    {
        public uint SerialID { get; }

        private static uint ID_ = 1;

        protected ObjectBase()
        {
            SerialID = ID_++;
        }
    }
}