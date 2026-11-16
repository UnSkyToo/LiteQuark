using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CreateAssetMenu(fileName = "ResSetting", menuName = "LiteQuark/ResCollector Setting")]
    public class ResCollectorSetting : ScriptableObject
    {
        [SerializeField]
        public List<string> IgnorePathList = new();

        private const string RES_SETTING_ASSET_PATH = "Assets/Editor/ResSetting.asset";

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

        internal static ResCollectorSetting LoadSetting()
        {
            var setting = AssetDatabase.LoadAssetAtPath<ResCollectorSetting>(RES_SETTING_ASSET_PATH);
            return setting != null ? setting : FindSettingPreferences();
        }

        // 仅供明确的配置编辑操作调用；收集和 Inspector 绘制使用 LoadSetting。
        internal static ResCollectorSetting CreateSetting()
        {
            var setting = LoadSetting();
            if (setting != null)
            {
                return setting;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            {
                AssetDatabase.CreateFolder("Assets", "Editor");
            }
            setting = CreateInstance<ResCollectorSetting>();
            AssetDatabase.CreateAsset(setting, RES_SETTING_ASSET_PATH);
            return setting;
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
