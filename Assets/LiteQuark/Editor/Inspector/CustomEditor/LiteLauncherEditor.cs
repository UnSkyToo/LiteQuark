using System;
using System.Collections.Generic;
using System.Linq;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomEditor(typeof(LiteLauncher))]
    public sealed class LiteLauncherEditor : UnityEditor.Editor
    {
        private static readonly string[] TabLabels = { "Debug", "Release" };
        private static readonly string[] SettingPropertyNames = { "DebugSetting", "ReleaseSetting" };

        private struct ProfileProperties
        {
            public SerializedProperty LogicList;
            public SerializedProperty SystemList;
            public SerializedProperty Common;
            public SerializedProperty Task;
            public SerializedProperty Asset;
            public SerializedProperty Action;
            public SerializedProperty Log;
            public SerializedProperty SystemSettings;
        }

        private ProfileProperties[] _profiles;
        private SerializedProperty _simulateReleaseBuildProperty;
        private int _selectedTab;
        private readonly Dictionary<string, bool> _foldoutStates = new();
        private readonly string[] _lastSystemListSnapshots = { string.Empty, string.Empty };

        private void OnEnable()
        {
            _simulateReleaseBuildProperty = serializedObject.FindProperty("_simulateReleaseBuild");
            _profiles = new ProfileProperties[2];
            for (var i = 0; i < 2; i++)
            {
                var root = serializedObject.FindProperty(SettingPropertyNames[i]);
                _profiles[i] = new ProfileProperties
                {
                    LogicList = root.FindPropertyRelative("LogicList"),
                    SystemList = root.FindPropertyRelative("SystemList"),
                    Common = root.FindPropertyRelative("Common"),
                    Task = root.FindPropertyRelative("Task"),
                    Asset = root.FindPropertyRelative("Asset"),
                    Action = root.FindPropertyRelative("Action"),
                    Log = root.FindPropertyRelative("Log"),
                    SystemSettings = root.FindPropertyRelative("SystemSettings"),
                };
                _lastSystemListSnapshots[i] = GetSystemListSnapshot(i);
            }

            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (Application.isPlaying)
            {
                DrawRuntimeStatus();
            }

            EditorGUILayout.PropertyField(_simulateReleaseBuildProperty, new GUIContent("Simulate Release Build"));
            EditorGUILayout.Space(5);

            var isSimulateRelease = _simulateReleaseBuildProperty.boolValue;
            if (isSimulateRelease)
            {
                _selectedTab = 1;
            }

            using (new EditorGUI.DisabledScope(isSimulateRelease))
            {
                _selectedTab = GUILayout.Toolbar(_selectedTab, TabLabels);
            }
            EditorGUILayout.Space(5);

            var currentSnapshot = GetSystemListSnapshot(_selectedTab);
            if (currentSnapshot != _lastSystemListSnapshots[_selectedTab])
            {
                SyncSystemSettings(_selectedTab);
                _lastSystemListSnapshots[_selectedTab] = currentSnapshot;
            }

            var props = _profiles[_selectedTab];

            EditorGUILayout.PropertyField(props.LogicList);
            EditorGUILayout.PropertyField(props.SystemList);

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(props.Common);
            EditorGUILayout.PropertyField(props.Task);
            EditorGUILayout.PropertyField(props.Asset);
            EditorGUILayout.PropertyField(props.Action);
            EditorGUILayout.PropertyField(props.Log);

            DrawAddonSystemSettings(props.SystemSettings);

            DrawSettingValidation();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAddonSystemSettings(SerializedProperty systemSettingsProperty)
        {
            if (systemSettingsProperty == null || systemSettingsProperty.arraySize == 0)
            {
                return;
            }

            EditorGUILayout.Space(10);

            for (var i = 0; i < systemSettingsProperty.arraySize; i++)
            {
                var settingProperty = systemSettingsProperty.GetArrayElementAtIndex(i);

                if (settingProperty.managedReferenceValue == null)
                    continue;

                var settingType = settingProperty.managedReferenceValue.GetType();
                var displayName = TypeUtils.GetTypeDisplayName(settingType);
                var foldoutKey = $"{_selectedTab}_{settingType.FullName ?? settingType.Name}";

                if (!_foldoutStates.TryGetValue(foldoutKey, out var foldout))
                {
                    _foldoutStates[foldoutKey] = true;
                }

                EditorGUILayout.Space(2);
                _foldoutStates[foldoutKey] = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, displayName);

                if (_foldoutStates[foldoutKey])
                {
                    EditorGUI.indentLevel++;

                    DrawSettingFields(settingProperty);

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }

        private void DrawSettingFields(SerializedProperty settingProperty)
        {
            var iterator = settingProperty.Copy();
            var endProperty = settingProperty.GetEndProperty();

            if (iterator.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(iterator, endProperty))
                    {
                        break;
                    }

                    EditorGUILayout.PropertyField(iterator, true);
                }
                while (iterator.NextVisible(false));
            }
        }

        private void DrawRuntimeStatus()
        {
            var runtime = LiteRuntime.Instance;
            var state = runtime.State;
            var profile = _simulateReleaseBuildProperty.boolValue ? "Release" : "Debug";
            var status = $"State: {state} | Profile: {profile} | DebugMode: {(LiteRuntime.IsDebugMode ? "ON" : "OFF")}";

            if (runtime.IsPause)
            {
                status += " | Paused";
            }

            var messageType = state switch
            {
                LiteRuntime.RuntimeState.Running => MessageType.Info,
                LiteRuntime.RuntimeState.Error => MessageType.Error,
                _ => MessageType.Warning,
            };

            EditorGUILayout.HelpBox(status, messageType);
            EditorGUILayout.Space(5);
        }

        private void DrawSettingValidation()
        {
            var launcher = target as LiteLauncher;
            var setting = _selectedTab == 0 ? launcher?.DebugSetting : launcher?.ReleaseSetting;
            if (setting == null)
                return;

            var asset = setting.Asset;

            if (asset.AssetMode == AssetProviderMode.Bundle &&
                asset.BundleLocater == BundleLocaterMode.Remote &&
                string.IsNullOrWhiteSpace(asset.BundleRemoteUri))
            {
                EditorGUILayout.HelpBox("BundleRemoteUri is empty while using Remote mode.", MessageType.Warning);
            }

            if (asset.AssetMode == AssetProviderMode.Bundle && asset.EnableRetain)
            {
                if (asset.AssetRetainTime <= 0)
                {
                    EditorGUILayout.HelpBox("AssetRetainTime should be greater than 0 when Retain is enabled.", MessageType.Warning);
                }

                if (asset.BundleRetainTime <= 0)
                {
                    EditorGUILayout.HelpBox("BundleRetainTime should be greater than 0 when Retain is enabled.", MessageType.Warning);
                }
            }
        }

        private void SyncSystemSettings(int tabIndex)
        {
            var launcher = target as LiteLauncher;
            var setting = tabIndex == 0 ? launcher?.DebugSetting : launcher?.ReleaseSetting;
            if (setting == null)
                return;

            var newSettingList = new List<ISystemSetting>();

            foreach (var entry in setting.SystemList)
            {
                if (entry.Disabled || string.IsNullOrWhiteSpace(entry.AssemblyQualifiedName))
                {
                    continue;
                }

                var systemType = Type.GetType(entry.AssemblyQualifiedName);
                var settingType = GetSettingTypeFromSystem(systemType);
                if (settingType == null)
                {
                    continue;
                }

                var existingSetting = setting.SystemSettings?.FirstOrDefault(s => s != null && s.GetType() == settingType);

                if (existingSetting != null)
                {
                    newSettingList.Add(existingSetting);
                }
                else
                {
                    var newSetting = Activator.CreateInstance(settingType) as ISystemSetting;
                    newSettingList.Add(newSetting);
                }
            }

            setting.SystemSettings = newSettingList;

            EditorUtility.SetDirty(target);
        }

        private static Type GetSettingTypeFromSystem(Type systemType)
        {
            var settingInterface = systemType?.GetInterfaces()
                .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISystemSettingProvider<>));

            return settingInterface?.GetGenericArguments()[0];
        }

        private string GetSystemListSnapshot(int tabIndex)
        {
            var launcher = target as LiteLauncher;
            var setting = tabIndex == 0 ? launcher?.DebugSetting : launcher?.ReleaseSetting;
            if (setting?.SystemList == null)
                return string.Empty;

            var entries = setting.SystemList
                .Where(static e => !e.Disabled && !string.IsNullOrWhiteSpace(e.AssemblyQualifiedName))
                .Select(static e => e.AssemblyQualifiedName);

            return string.Join("|", entries);
        }
    }
}
