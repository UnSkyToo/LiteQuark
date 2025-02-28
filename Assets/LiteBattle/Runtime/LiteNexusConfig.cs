using LiteQuark.Runtime;
using UnityEngine;
#if UNITY_EDITOR
using LiteQuark.Editor;
using UnityEditor;
#endif

namespace LiteBattle.Runtime
{
    public class LiteNexusConfig : ScriptableObject
    {
        private const string ConfigPath = "Assets/StandaloneAssets";
        private const string ConfigName = "LiteNexusConfig.asset";

        /// <summary>
        /// 开启快速热重启后，修改可以直接生效，不用点击Reload按钮
        /// </summary>
        [SerializeField]
        public bool QuickReload = false;
        
        [SerializeField]
        public string DatabasePath = "Assets/StandaloneAssets/NexusDatabase";
        
        private static LiteNexusConfig Instance_;
        public static LiteNexusConfig Instance
        {
            get
            {
                if (Instance_ == null)
                {
#if UNITY_EDITOR
                    var configPath = PathUtils.ConcatPath(ConfigPath, ConfigName);
                    if (!PathUtils.FileExists(configPath))
                    {
                        CreateNexusConfig();
                    }

                    Instance_ = AssetDatabase.LoadAssetAtPath<LiteNexusConfig>(configPath);
#else
                    Instance_ = LiteRuntime.Asset.LoadAssetSync<LiteNexusConfig>(ConfigName);
#endif
                }
                
                Debug.Assert(Instance_ != null);
                return Instance_;
            }
        }
        
#if UNITY_EDITOR
        [MenuItem("Lite/Nexus Engine/Create Nexus Config")]
        public static void CreateNexusConfig()
        {
            AssetUtils.CreateAsset<LiteNexusConfig>(ConfigPath, ConfigName);
        }
#endif
        
        private string GetDatabaseFullPath(string subPath)
        {
            return PathUtils.ConcatPath(DatabasePath, subPath);
        }

        public string GetDatabaseJsonPath()
        {
            return GetDatabaseFullPath("Nexus.json");
        }

        public string GetUnitDatabasePath()
        {
            return GetDatabaseFullPath("Unit");
        }

        public string GetTimelineDatabasePath()
        {
            return GetDatabaseFullPath("Timeline");
        }
    }
}