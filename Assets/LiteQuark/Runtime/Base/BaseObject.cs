namespace LiteQuark.Runtime
{
    public abstract class BaseObject
    {
        public uint SerialID { get; }

        private static uint ID_ = 1;

        protected BaseObject()
        {
            SerialID = ID_++;
        }
    }
}