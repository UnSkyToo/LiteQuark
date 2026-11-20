namespace LiteQuark.Runtime
{
    public static class LiteConst
    {
        public const string Tag = "LiteQuark";
        public const bool SecurityMode = true;
        public const string SecurityKey = "li1" + "te9" + "qu9" + "ark1";
        public const string AssetRootPath = "Assets/StandaloneAssets";
        public const string AssetRootName = "Assets";
        public const string BundleFileExt = ".ab";

        public static readonly char[] InvalidFileNameChars = new char[] { '&', '?' };

        public const float MinIntervalTime = 0.00001f;

        public static class TaskGroupId
        {
            public const int Default = 0;
            public const int Flow = -1;
            public const int Asset = -2;
        }
    }

    public enum FrameworkErrorCode
    {
        Unknown,
        Startup,
        LoadVersionPack,
        LoadRemoteBundle,
    }
}
