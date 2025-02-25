using UnityEditor;

namespace LiteQuark.Editor
{
    internal static class UIInitializer
    {
        [InitializeOnLoadMethod]
        private static void SetDefineSymbol()
        {
            LiteEditorUtils.AddScriptingDefineSymbols("LITE_QUARK_ENABLE_UI");
        }
    }
}