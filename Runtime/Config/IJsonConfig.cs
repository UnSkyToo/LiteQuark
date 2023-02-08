using System;

namespace LiteQuark.Runtime
{
    public interface IJsonConfig : ICloneable
    {
    }
    
    public interface IJsonMainID
    {
        int GetMainID();
    }
    
    public interface IJsonMainConfig : IJsonConfig, IJsonMainID
    {
        int ID { get; }
        string Name { get; }
    }
}