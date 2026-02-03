using System;
using System.Collections.Generic;
using System.Linq;
using LiteQuark.Runtime;
using UnityEditor;

namespace LiteQuark.Editor
{
    [CustomEditor(typeof(LiteLauncher))]
    public sealed class LiteLauncherEditor : UnityEditor.Editor
    {
        private SerializedProperty _settingProperty;
        
        private SerializedProperty _logicListProperty;
        private SerializedProperty _systemListProperty;
        private SerializedProperty _commonProperty;
        private SerializedProperty _taskProperty;
        private SerializedProperty _assetProperty;
        private SerializedProperty _actionProperty;
        private SerializedProperty _logProperty;
        private SerializedProperty _systemSettingsProperty;
        
        private readonly Dictionary<string, bool> _foldoutStates = new();
        private string _lastSystemListSnapshot;

        private void OnEnable()
        {
            _settingProperty = serializedObject.FindProperty("Setting");

            _logicListProperty = _settingProperty.FindPropertyRelative("LogicList");
            _systemListProperty = _settingProperty.FindPropertyRelative("SystemList");
            _commonProperty = _settingProperty.FindPropertyRelative("Common");
            _taskProperty = _settingProperty.FindPropertyRelative("Task");
            _assetProperty = _settingProperty.FindPropertyRelative("Asset");
            _actionProperty = _settingProperty.FindPropertyRelative("Action");
            _logProperty = _settingProperty.FindPropertyRelative("Log");
            _systemSettingsProperty = _settingProperty.FindPropertyRelative("SystemSettings");
            
            _lastSystemListSnapshot = GetSystemListSnapshot();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var currentSnapshot = GetSystemListSnapshot();
            if (currentSnapshot != _lastSystemListSnapshot)
            {
                SyncSystemSettings();
                _lastSystemListSnapshot = currentSnapshot;
            }
            
            EditorGUILayout.PropertyField(_logicListProperty);
            EditorGUILayout.PropertyField(_systemListProperty);

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_commonProperty);
            EditorGUILayout.PropertyField(_taskProperty);
            EditorGUILayout.PropertyField(_assetProperty);
            EditorGUILayout.PropertyField(_actionProperty);
            EditorGUILayout.PropertyField(_logProperty);
            
            DrawAddonSystemSettings();

            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawAddonSystemSettings()
        {
            if (_systemSettingsProperty == null || _systemSettingsProperty.arraySize == 0)
            {
                return;
            }

            EditorGUILayout.Space(10);

            for (var i = 0; i < _systemSettingsProperty.arraySize; i++)
            {
                var settingProperty = _systemSettingsProperty.GetArrayElementAtIndex(i);

                if (settingProperty.managedReferenceValue == null)
                    continue;

                var settingType = settingProperty.managedReferenceValue.GetType();
                var displayName = TypeUtils.GetTypeDisplayName(settingType);
                var foldoutKey = settingType.FullName ?? settingType.Name;
                
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
        
        private void SyncSystemSettings()
        {
            var launcher = target as LiteLauncher;
            if (launcher?.Setting == null)
                return;

            var setting = launcher.Setting;
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
        
        private Type GetSettingTypeFromSystem(Type systemType)
        {
            var settingInterface = systemType?.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISystemSettingProvider<>));
            
            return settingInterface?.GetGenericArguments()[0];
        }
        
        private string GetSystemListSnapshot()
        {
            var launcher = target as LiteLauncher;
            if (launcher?.Setting?.SystemList == null)
                return string.Empty;
            
            var entries = launcher.Setting.SystemList
                .Where(e => !e.Disabled && !string.IsNullOrWhiteSpace(e.AssemblyQualifiedName))
                .Select(e => e.AssemblyQualifiedName);

            return string.Join("|", entries);
        }
    }
}