namespace LiteCard.GamePlay
{
    public abstract class ObjectBase
    {
        public int UniqueID { get; }
        
        private static int IDGenerator_ = 1;

        protected ObjectBase()
        {
            UniqueID = IDGenerator_++;
        }
    }
}