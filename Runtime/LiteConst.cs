using System;

namespace LiteQuark.Runtime
{
    public static class LiteConst
    {
        public const string AssetRootPath = "Assets/StandaloneAssets";
        public const string BundlePackFileName = "bundle_pack.bytes";

        public const float MinIntervalTime = 0.00001f;

        public static readonly Type[] SystemTypeList =
        {
            typeof(LogSystem),
            typeof(EventSystem),
            typeof(TaskSystem),
            typeof(TimerSystem),
            typeof(GroupSystem),
            typeof(AssetSystem),
            typeof(ObjectPoolSystem),
            typeof(AudioSystem),
            typeof(ActionSystem),
            typeof(UISystem),
        };
    }
}