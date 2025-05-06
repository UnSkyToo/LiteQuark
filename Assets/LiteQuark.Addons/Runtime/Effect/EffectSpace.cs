using System;

namespace LiteQuark.Runtime
{
    [Flags]
    public enum EffectSpace
    {
        Auto = 0,
        LocalPosition = 1 << 0,
        LocalRotation = 1 << 1,
        WorldPosition = 1 << 2,
        WorldRotation = 1 << 3,
        Local = LocalPosition | LocalRotation,
        World = WorldPosition | WorldRotation,
    }
}