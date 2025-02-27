namespace LiteBattle.Runtime
{
    public sealed class LiteStateConfig
    {
        public string Name { get; private set; }
        public LiteClip[] Clips { get; private set; }
        
        public LiteStateConfig(string name, LiteClip[] clips)
        {
            Name = name;
            Clips = clips;
        }
    }
}