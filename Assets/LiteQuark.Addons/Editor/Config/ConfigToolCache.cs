using UnityEditor;

namespace LiteQuark.Editor
{
    internal static class ConfigToolCache
    {
        private const string KeyCache = "LiteQuark.ConfigTool.Cache";
        
        private const string KeyExcelFolder = "LiteQuark.ConfigTool.ExcelFolder";
        private const string KeyJsonOutputFolder = "LiteQuark.ConfigTool.JsonOutputFolder";
        private const string KeyCodeOutputFolder = "LiteQuark.ConfigTool.CodeOutputFolder";
        private const string KeyCodeNamespace = "LiteQuark.ConfigTool.CodeNamespace";
        private const string KeyJsonBasePath = "LiteQuark.ConfigTool.JsonBasePath";

        private const string DefaultJsonOutputFolder = "Assets/StandaloneAssets/Configs";
        private const string DefaultCodeOutputFolder = "Assets/GamePlay/Config";
        private const string DefaultCodeNamespace = "GamePlay";
        private const string DefaultJsonBasePath = "Configs";
        
        public static string Cache
        {
            get => EditorPrefs.GetString(KeyCache, string.Empty);
            set => EditorPrefs.SetString(KeyCache, value);
        }

        public static string ExcelFolder
        {
            get => EditorPrefs.GetString(KeyExcelFolder, string.Empty);
            set => EditorPrefs.SetString(KeyExcelFolder, value);
        }

        public static string JsonOutputFolder
        {
            get => EditorPrefs.GetString(KeyJsonOutputFolder, DefaultJsonOutputFolder);
            set => EditorPrefs.SetString(KeyJsonOutputFolder, value);
        }

        public static string CodeOutputFolder
        {
            get => EditorPrefs.GetString(KeyCodeOutputFolder, DefaultCodeOutputFolder);
            set => EditorPrefs.SetString(KeyCodeOutputFolder, value);
        }

        public static string CodeNamespace
        {
            get => EditorPrefs.GetString(KeyCodeNamespace, DefaultCodeNamespace);
            set => EditorPrefs.SetString(KeyCodeNamespace, value);
        }

        public static string JsonBasePath
        {
            get => EditorPrefs.GetString(KeyJsonBasePath, DefaultJsonBasePath);
            set => EditorPrefs.SetString(KeyJsonBasePath, value);
        }
    }
}