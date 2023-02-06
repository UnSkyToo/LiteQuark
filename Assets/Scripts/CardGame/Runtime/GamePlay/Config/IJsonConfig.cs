﻿using System;

namespace LiteCard.GamePlay
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