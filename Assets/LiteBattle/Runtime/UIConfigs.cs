using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    public static class UIConfigs
    {
        public static readonly UIConfig UIFloatText = new UIConfig("demo/Prefabs/FloatText.prefab", UIDepthMode.Scene, false, false);
        public static readonly UIConfig UINameplateHUD = new UIConfig("demo/Prefabs/Nameplate.prefab", UIDepthMode.Scene, false, false);
    }
}