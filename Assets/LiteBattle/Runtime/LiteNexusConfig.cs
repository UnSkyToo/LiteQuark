using LiteQuark.Runtime;
using Cysharp.Threading.Tasks;
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
        private static AssetHandle<LiteNexusConfig> ConfigHandle_;
        public static LiteNexusConfig Instance
        {
            get
            {
                if (Instance_ == null)
                {
#if UNITY_EDITOR
                    var configPath = PathUtils.ConcatPath(ConfigPath, ConfigName);
                    if (!System.IO.File.Exists(configPath))
                    {
                        CreateNexusConfig();
                    }

                    Instance_ = AssetDatabase.LoadAssetAtPath<LiteNexusConfig>(configPath);
#else
                    throw new System.InvalidOperationException("Initialize LiteNexusDataManager before accessing LiteNexusConfig.Instance.");
#endif
                }

                Debug.Assert(Instance_ != null);
                return Instance_;
            }
        }

        internal static async UniTask<LiteNexusConfig> LoadAsync()
        {
#if UNITY_EDITOR
            return await UniTask.FromResult(Instance);
#else
            ConfigHandle_ ??= LiteRuntime.Asset.LoadAssetHandle<LiteNexusConfig>(ConfigName);
            var handle = ConfigHandle_;
            try
            {
                await handle;
                if (ConfigHandle_ != handle)
                {
                    return null;
                }

                Instance_ = handle.Result;
                if (Instance_ == null)
                {
                    Release();
                }
                return Instance_;
            }
            catch
            {
                if (ConfigHandle_ == handle)
                {
                    Release();
                }
                throw;
            }
#endif
        }

        internal static void Release()
        {
            ConfigHandle_?.Dispose();
            ConfigHandle_ = null;
            Instance_ = null;
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
