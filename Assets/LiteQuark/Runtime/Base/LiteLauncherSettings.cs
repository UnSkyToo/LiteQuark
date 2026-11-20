using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LiteQuark.Runtime
{
    [Serializable]
    public sealed class LiteLauncherSettings
    {
        public string GameEntry;
        public LiteSetting.CommonSetting Common = new();
        public LiteSetting.TaskSetting Task = new();
        public LiteSetting.AssetSetting Asset = new();
        public LiteSetting.ActionSetting Action = new();
        public LiteSetting.LogSetting Log = new();
        public List<LiteSystemEntry> SystemList = new();

        public LiteSetting CreateRuntimeSetting(LiteBuildProfile profile)
        {
            // Unity's same-session serialization copies managed settings and retains scene object references.
            var copy = JsonUtility.FromJson<LiteLauncherSettings>(JsonUtility.ToJson(this));
            return new LiteSetting(profile, copy);
        }
    }

    [Serializable]
    public sealed class LiteSystemEntry
    {
        public bool Disabled;
        public string AssemblyQualifiedName;
        [SerializeReference] public ISystemSetting Setting;

        internal static Type GetProviderInterface(Type systemType)
        {
            return systemType.GetInterfaces().FirstOrDefault(static type =>
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISystemSettingProvider<>));
        }
    }
}
