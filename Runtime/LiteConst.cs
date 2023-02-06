using System;

namespace LiteQuark.Runtime
{
    public static class LiteConst
    {
        public const string AssetRootPath = "Assets/StandaloneAssets";
        public const string BundlePackFileName = "bundle_pack.bytes";

        public static readonly Type[] SystemTypeList =
        {
            typeof(LogSystem),
            typeof(ObjectPoolSystem),
            typeof(EventSystem),
            typeof(TaskSystem),
            typeof(TimerSystem),
            typeof(GroupSystem),
            typeof(AssetSystem),
            typeof(AudioSystem),
            typeof(UISystem),
        };
    }
}