using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public static class LiteConst
    {
        public const string Tag = "LiteQuark";
        public const string AssetRootPath = "Assets/StandaloneAssets";
        public const string BundlePackFileName = "bundle_pack.bytes";

        public const float MinIntervalTime = 0.00001f;

        public static readonly Dictionary<string, int> InternalSystem = new Dictionary<string, int>
        {
            { typeof(LogSystem).AssemblyQualifiedName, 99900 },
            { typeof(EventSystem).AssemblyQualifiedName, 99800 },
            { typeof(TaskSystem).AssemblyQualifiedName, 99700 },
            { typeof(TimerSystem).AssemblyQualifiedName, 99600 },
            { typeof(GroupSystem).AssemblyQualifiedName, 99500 },
            { typeof(AssetSystem).AssemblyQualifiedName, 99400 },
            { typeof(ObjectPoolSystem).AssemblyQualifiedName, 99300 },
            { typeof(AudioSystem).AssemblyQualifiedName, 99200 },
            { typeof(EffectSystem).AssemblyQualifiedName, 99100 },
            { typeof(ActionSystem).AssemblyQualifiedName, 99000 },
        };
    }
}