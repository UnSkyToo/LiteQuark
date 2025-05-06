namespace LiteQuark.Runtime
{
    public struct UIConfig
    {
        public string PrefabPath { get; }
        public UIDepthMode DepthMode { get; }
        public bool IsMutex { get; }
        public bool AutoAdapt { get; }

        public UIConfig(string prefabPath, UIDepthMode depthMode, bool isMutex = true, bool autoAdapt = true)
        {
            PrefabPath = prefabPath;
            DepthMode = depthMode;
            IsMutex = isMutex;
            AutoAdapt = autoAdapt;
        }
    }
}