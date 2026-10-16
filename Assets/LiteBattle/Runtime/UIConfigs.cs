using LiteQuark.Runtime;

namespace LiteBattle.Runtime
{
    public static class UIConfigs
    {
        public static readonly UIConfig UIFloatText = new("demo/Prefabs/FloatText.prefab", UIDepthMode.Scene, false, false);
        public static readonly UIConfig UINameplateHUD = new("demo/Prefabs/Nameplate.prefab", UIDepthMode.Scene, false, false);
    }
}