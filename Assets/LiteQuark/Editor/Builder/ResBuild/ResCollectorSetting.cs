using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CreateAssetMenu(fileName = "ResSetting", menuName = "LiteQuark/ResCollector Setting")]
    public class ResCollectorSetting : ScriptableObject
    {
        [SerializeField]
        public List<string> IgnorePathList = new List<string>();
        
        private const string RES_SETTING_ASSET_PATH = "Assets/Editor/ResSetting.asset";
        private static ResCollectorSetting _setting = null;
        private static int _wasPreferencesDirCreated = 0;
        private static int _wasPreferencesAssetCreated = 0;

        public bool IsIgnorePath(string path)
        {
            return IgnorePathList.Contains(path);
        }

        public void SetIgnorePath(string path, bool ignore)
        {
            if (ignore)
            {
                if (!IgnorePathList.Contains(path))
                {
                    IgnorePathList.Add(path);
                }
            }
            else
            {
                IgnorePathList.Remove(path);
            }
        }
        
        internal static ResCollectorSetting GetOrCreateSetting()
        {
            if (_setting != null)
            {
                return _setting;
            }

            _setting = AssetDatabase.LoadAssetAtPath<ResCollectorSetting>(RES_SETTING_ASSET_PATH);
            if (_setting == null)
            {
                _setting = FindSettingPreferences();
            }
            if (_setting == null)
            {
                _setting = CreateInstance<ResCollectorSetting>();
                if (!AssetDatabase.IsValidFolder("Assets/Editor") && Interlocked.Exchange(ref _wasPreferencesDirCreated, 1) == 0)
                {
                    AssetDatabase.CreateFolder("Assets", "Editor");
                }
                if (Interlocked.Exchange(ref _wasPreferencesAssetCreated, 1) == 0)
                {
                    AssetDatabase.CreateAsset(_setting, RES_SETTING_ASSET_PATH);
                }
            }
            return _setting;
        }

        private static ResCollectorSetting FindSettingPreferences()
        {
            var typeSearchString = $" t:{nameof(ResCollectorSetting)}";
            var guids = AssetDatabase.FindAssets(typeSearchString);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var setting = AssetDatabase.LoadAssetAtPath<ResCollectorSetting>(path);
                if (setting != null)
                {
                    return setting;
                }
            }
            return null;
        }
    }
}