namespace LiteBattle.Runtime
{
    public sealed class LiteStateData
    {
        public string Name { get; }
        public LiteClip[] Clips { get; }
        
        public LiteStateData(string name, LiteClip[] clips)
        {
            Name = name;
            Clips = clips;
        }
    }
}