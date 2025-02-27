using LiteQuark.Runtime;
using UnityEngine;
#if UNITY_EDITOR
using LiteQuark.Editor;
using UnityEditor;
#endif

namespace LiteBattle.Runtime
{
    public class LiteStateConfig : ScriptableObject
    {
        private const string ConfigPath = "Assets/StandaloneAssets";
        private const string ConfigName = "LiteStateConfig.asset";

        [SerializeField]
        public string DatabasePath = "Assets/StandaloneAssets/StateDatabase";
        
        private static LiteStateConfig Instance_;
        public static LiteStateConfig Instance
        {
            get
            {
                if (Instance_ == null)
                {
#if UNITY_EDITOR
                    var configPath = PathUtils.ConcatPath(ConfigPath, ConfigName);
                    if (!PathUtils.FileExists(configPath))
                    {
                        CreateStateConfig();
                    }

                    Instance_ = AssetDatabase.LoadAssetAtPath<LiteStateConfig>(configPath);
#else
                    Instance_ = LiteRuntime.Asset.LoadAssetSync<LiteStateConfig>(ConfigName);
#endif
                }
                
                Debug.Assert(Instance_ != null);
                return Instance_;
            }
        }
        
#if UNITY_EDITOR
        [MenuItem("Lite/State Engine/Create State Config")]
        public static void CreateStateConfig()
        {
            AssetUtils.CreateAsset<LiteStateConfig>(ConfigPath, ConfigName);
        }
#endif
        
        private string GetDatabaseFullPath(string subPath)
        {
            return PathUtils.ConcatPath(DatabasePath, subPath);
        }

        public string GetDatabaseJsonPath()
        {
            return GetDatabaseFullPath("Database.json");
        }

        public string GetAgentDatabasePath()
        {
            return GetDatabaseFullPath("Agent");
        }

        public string GetTimelineDatabasePath()
        {
            return GetDatabaseFullPath("Timeline");
        }
    }
}