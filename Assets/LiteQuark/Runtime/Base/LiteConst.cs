using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public static class LiteConst
    {
        public const string Tag = "LiteQuark";
        public const bool SecurityMode = true;
        public const string SecurityKey = "li1" + "te9" + "qu9" + "ark1";
        public const string AssetRootPath = "Assets/StandaloneAssets";
        public const string BundleFileExt = ".ab";

        public static readonly char[] InvalidFileNameChars = new char[] { '&', '?' };

        public const float MinIntervalTime = 0.00001f;

        public static readonly Dictionary<string, int> InternalSystem = new Dictionary<string, int>
        {
            { typeof(LogSystem).AssemblyQualifiedName, 99900 },
            { typeof(EventSystem).AssemblyQualifiedName, 99800 },
            { typeof(TaskSystem).AssemblyQualifiedName, 99700 },
            { typeof(TimerSystem).AssemblyQualifiedName, 99600 },
            { typeof(AssetSystem).AssemblyQualifiedName, 99500 },
            { typeof(ObjectPoolSystem).AssemblyQualifiedName, 99400 },
            { typeof(ActionSystem).AssemblyQualifiedName, 99300 },
        };
    }
}