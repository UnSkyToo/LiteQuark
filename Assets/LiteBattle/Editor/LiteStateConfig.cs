using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteBattle.Editor
{
    public class LiteStateConfig : ScriptableObject
    {
        private const string ConfigName = "LiteStateConfig.asset";

        [SerializeField]
        public string DataPath = "Assets/StandaloneAssets/StateData";

        public static LiteStateConfig Instance
        {
            get
            {
                var configPath = GetConfigPath();
                if (!PathUtils.FileExists(configPath))
                {
                    CreateStateConfig();
                }
                return AssetDatabase.LoadAssetAtPath<LiteStateConfig>(configPath);
            }
        }
        
        [MenuItem("Lite/State/Create State Config")]
        public static void CreateStateConfig()
        {
            var config = CreateInstance<LiteStateConfig>();
            config.name = ConfigName;
            AssetDatabase.CreateAsset(config, GetConfigPath());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string GetConfigPath()
        {
            return $"Assets/{ConfigName}";
        }
    }
}