using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LiteQuark.Editor
{
    [CustomEditor(typeof(LiteLauncher))]
    public sealed class LiteLauncherEditor : UnityEditor.Editor
    {
        private static readonly string[] ProfileTabs = { "Release", "Development" };
        private Type[] _systemTypes;
        private string[] _gameEntryNames;
        private GUIContent[] _gameEntryLabels;
        private ReorderableList _systems;

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        private static Type[] SelectableTypes<T>()
        {
            return TypeCache.GetTypesDerivedFrom<T>()
                .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters &&
                    !typeof(UnityEngine.Object).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null &&
                    !type.IsDefined(typeof(LiteHideTypeAttribute), true))
                .OrderBy(type => type.FullName, StringComparer.Ordinal).ToArray();
        }

        private void OnEnable()
        {
            var entries = SelectableTypes<IGameEntry>();
            _gameEntryNames = entries.Select(type => type.AssemblyQualifiedName).Prepend(string.Empty).ToArray();
            _gameEntryLabels = entries.Select(type => new GUIContent(type.FullName)).Prepend(new GUIContent("None")).ToArray();
            _systemTypes = SelectableTypes<ISystem>();
            var list = serializedObject.FindProperty("Settings.SystemList");
            _systems = new ReorderableList(serializedObject, list, true, true, true, true);
            _systems.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "System List");
            _systems.elementHeightCallback = index => EntryHeight(list.GetArrayElementAtIndex(index));
            _systems.drawElementCallback = (rect, index, active, focused) => DrawEntry(rect, list.GetArrayElementAtIndex(index));
            _systems.onAddDropdownCallback = (rect, _) => ShowSystemMenu(rect);
        }

        public override void OnInspectorGUI()
        {
            using var labelWidth = new LabelWidthScope(Mathf.Min(245f, EditorGUIUtility.currentViewWidth * 0.44f));
            serializedObject.Update();
            var settings = serializedObject.FindProperty(nameof(LiteLauncher.Settings));
            var profile = DrawEnvironment();
            var development = profile == LiteBuildProfile.Development;
            using (new EditorGUI.DisabledScope(development))
            {
                DrawGameEntry(settings.FindPropertyRelative(nameof(LiteLauncherSettings.GameEntry)));
            }
            EditorGUILayout.Space();

            DrawModule(settings.FindPropertyRelative(nameof(LiteLauncherSettings.Common)), development);
            DrawModule(settings.FindPropertyRelative(nameof(LiteLauncherSettings.Task)), development);
            DrawModule(settings.FindPropertyRelative(nameof(LiteLauncherSettings.Asset)), development);
            DrawModule(settings.FindPropertyRelative(nameof(LiteLauncherSettings.Action)), development);
            DrawModule(settings.FindPropertyRelative(nameof(LiteLauncherSettings.Log)), development);
            EditorGUILayout.Space();
            if (development)
            {
                DrawDevelopmentSystems(settings.FindPropertyRelative(nameof(LiteLauncherSettings.SystemList)));
            }
            else
            {
                _systems.DoLayoutList();
            }
            serializedObject.ApplyModifiedProperties();

            if (NeedsCdnWarning(((LiteLauncher)target).Settings.Asset, profile))
            {
                EditorGUILayout.HelpBox($"{profile} uses Remote resources. Enter a valid absolute CDN URI under Asset.", MessageType.Warning);
            }
            if (Application.isPlaying && LiteRuntime.Instance.Launcher == target)
            {
                var runtime = LiteRuntime.Instance;
                var active = LiteRuntime.Setting;
                var error = runtime.State == LiteRuntime.RuntimeState.Error;
                EditorGUILayout.HelpBox($"{runtime.State}{(runtime.IsPause ? " (Paused)" : "")} · {active?.Environment}\nResources: {active?.Asset.BundleMode}   Log: {active?.Log.Levels}", error ? MessageType.Error : MessageType.Info);
            }
        }

        internal static bool NeedsCdnWarning(LiteSetting.AssetSetting asset, LiteBuildProfile profile)
        {
            if (asset.BundleModeByBuild.Resolve(profile) != BundleLoadMode.Remote) return false;
            var address = asset.BundleRemoteUriByBuild.Resolve(profile);
            return !Uri.TryCreate(address, UriKind.Absolute, out var uri) || !uri.IsWellFormedOriginalString();
        }

        private LiteBuildProfile DrawEnvironment()
        {
            var profile = serializedObject.FindProperty(nameof(LiteLauncher.EditorPreview));
            var rect = EditorGUILayout.GetControlRect();
            using var scope = new EditorGUI.PropertyScope(rect, new GUIContent("Environment", "Edit this environment and use it on the next Editor startup or restart."), profile);
            rect = EditorGUI.PrefixLabel(rect, scope.content);
            var current = (LiteBuildProfile)profile.enumValueIndex;
            var selected = GUI.Toolbar(rect, current == LiteBuildProfile.Release ? 0 : 1, ProfileTabs);
            var next = selected == 0 ? LiteBuildProfile.Release : LiteBuildProfile.Development;
            if (next != current)
            {
                GUI.FocusControl(null);
                EditorGUIUtility.editingTextField = false;
                profile.enumValueIndex = (int)next;
                serializedObject.ApplyModifiedProperties();
                Repaint();
                GUIUtility.ExitGUI();
            }
            return current;
        }

        private static void DrawModule(SerializedProperty module, bool development)
        {
            if (!development)
            {
                EditorGUILayout.PropertyField(module, true);
                return;
            }
            module.isExpanded = EditorGUILayout.Foldout(module.isExpanded, module.displayName, true);
            if (module.isExpanded)
            {
                using var indent = new EditorGUI.IndentLevelScope();
                DevelopmentSettingsFields.Visit(module, typeof(LiteLauncherSettings).GetField(module.name).FieldType, DrawDevelopmentField);
            }
        }

        private static bool DrawDevelopmentField(SerializedProperty property, FieldInfo field, DevelopmentSettingsFields.Access access, int depth)
        {
            using var indent = new EditorGUI.IndentLevelScope(depth);
            if (access == DevelopmentSettingsFields.Access.Override)
            {
                BuildValueDrawer.DrawDevelopmentLayout(property, field);
                return false;
            }
            if (access == DevelopmentSettingsFields.Access.Foldout)
            {
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, new GUIContent(property.displayName, property.tooltip), true);
                return property.isExpanded;
            }
            using (new EditorGUI.DisabledScope(true))
            {
                return EditorGUILayout.PropertyField(property, false);
            }
        }

        private static void DrawDevelopmentSystems(SerializedProperty list)
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("System List", EditorStyles.boldLabel);
            for (var i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                var disabled = entry.FindPropertyRelative(nameof(LiteSystemEntry.Disabled));
                var setting = entry.FindPropertyRelative(nameof(LiteSystemEntry.Setting));
                var expanded = DrawEntryHeader(EditorGUILayout.GetControlRect(), entry, true);
                if (NeedsSettingsRepair(entry, out var required))
                {
                    EditorGUILayout.HelpBox(RepairMessage(setting, required) + " Repair in Release.", MessageType.Warning);
                    continue;
                }
                if (!expanded) continue;
                using (new EditorGUI.IndentLevelScope())
                using (new EditorGUI.DisabledScope(disabled.boolValue))
                {
                    DevelopmentSettingsFields.Visit(setting, null, DrawDevelopmentField);
                }
            }
        }

        private static bool DrawEntryHeader(Rect rect, SerializedProperty entry, bool readOnly)
        {
            var disabled = entry.FindPropertyRelative(nameof(LiteSystemEntry.Disabled));
            using (new EditorGUI.DisabledScope(readOnly))
            {
                EditorGUI.BeginChangeCheck();
                var enabled = EditorGUI.Toggle(new Rect(rect.x, rect.y, 18f, rect.height), !disabled.boolValue);
                if (EditorGUI.EndChangeCheck()) disabled.boolValue = !enabled;
            }
            rect.xMin += 32f;
            var name = entry.FindPropertyRelative(nameof(LiteSystemEntry.AssemblyQualifiedName)).stringValue;
            var qualifiedName = name.Split(',')[0];
            var label = new GUIContent(qualifiedName.Substring(qualifiedName.LastIndexOf('.') + 1), name);
            if (entry.FindPropertyRelative(nameof(LiteSystemEntry.Setting)).managedReferenceValue == null)
            {
                EditorGUI.LabelField(rect, label);
                return false;
            }
            entry.isExpanded = EditorGUI.Foldout(rect, entry.isExpanded, label, true);
            return entry.isExpanded;
        }

        private void ShowSystemMenu(Rect rect)
        {
            var list = serializedObject.FindProperty("Settings.SystemList");
            var registered = new HashSet<string>();
            for (var i = 0; i < list.arraySize; i++)
            {
                registered.Add(list.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(LiteSystemEntry.AssemblyQualifiedName)).stringValue);
            }
            var menu = new GenericMenu();
            foreach (var type in _systemTypes)
            {
                var label = new GUIContent(type.FullName);
                if (registered.Contains(type.AssemblyQualifiedName))
                {
                    menu.AddDisabledItem(label, true);
                }
                else
                {
                    menu.AddItem(label, false, () =>
                    {
                        serializedObject.Update();
                        if (((LiteLauncher)target).EditorPreview != LiteBuildProfile.Release) return;
                        var registrations = serializedObject.FindProperty("Settings.SystemList");
                        AddSystem(registrations, type);
                        _systems.index = registrations.arraySize - 1;
                        serializedObject.ApplyModifiedProperties();
                        Repaint();
                    });
                }
            }
            if (_systemTypes.Length == 0) menu.AddDisabledItem(new GUIContent("No extension systems available"));
            menu.DropDown(rect);
        }

        internal static void AddSystem(SerializedProperty list, Type type)
        {
            var index = list.arraySize++;
            var entry = list.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative(nameof(LiteSystemEntry.Disabled)).boolValue = false;
            entry.FindPropertyRelative(nameof(LiteSystemEntry.AssemblyQualifiedName)).stringValue = type.AssemblyQualifiedName;
            SetEntrySettings(entry, RequiredSettings(type));
        }

        private static Type RequiredSettings(Type systemType) =>
            LiteSystemEntry.GetProviderInterface(systemType)?.GetGenericArguments()[0];

        internal static bool NeedsSettingsRepair(SerializedProperty entry, out Type required)
        {
            required = null;
            var name = entry.FindPropertyRelative(nameof(LiteSystemEntry.AssemblyQualifiedName)).stringValue;
            var type = string.IsNullOrWhiteSpace(name) ? null : Type.GetType(name, false);
            if (type == null) return false;
            required = RequiredSettings(type);
            var current = entry.FindPropertyRelative(nameof(LiteSystemEntry.Setting)).managedReferenceValue;
            return required == null ? current != null : !required.IsInstanceOfType(current);
        }

        internal static void SetEntrySettings(SerializedProperty entry, Type required)
        {
            entry.FindPropertyRelative(nameof(LiteSystemEntry.Setting)).managedReferenceValue =
                required == null ? null : Activator.CreateInstance(required);
            entry.isExpanded = required != null;
        }

        private static string RepairMessage(SerializedProperty setting, Type required)
        {
            var stored = setting.managedReferenceValue?.GetType().Name;
            if (required == null) return $"This system no longer uses {stored}.";
            return stored == null ? $"Missing {required.Name}." : $"Expected {required.Name}. Stored {stored}.";
        }

        private static float EntryHeight(SerializedProperty entry)
        {
            var height = EditorGUIUtility.singleLineHeight + 4f;
            var setting = entry.FindPropertyRelative(nameof(LiteSystemEntry.Setting));
            if (NeedsSettingsRepair(entry, out _))
            {
                height += RepairHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            if (setting.managedReferenceValue != null && entry.isExpanded)
            {
                DevelopmentSettingsFields.ForEachChild(setting, field => height += EditorGUI.GetPropertyHeight(field, true) + EditorGUIUtility.standardVerticalSpacing);
            }
            return height;
        }

        private static float RepairHeight => EditorGUIUtility.singleLineHeight * 2f;

        private static void DrawEntry(Rect rect, SerializedProperty entry)
        {
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            var disabled = entry.FindPropertyRelative(nameof(LiteSystemEntry.Disabled));
            var setting = entry.FindPropertyRelative(nameof(LiteSystemEntry.Setting));
            var expanded = DrawEntryHeader(rect, entry, false);
            rect.xMin += 32f;
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            var repair = NeedsSettingsRepair(entry, out var required);
            if (repair)
            {
                var button = new Rect(rect.xMax - 100f, rect.y, 100f, rect.height);
                var warning = new Rect(rect.x, rect.y, button.x - rect.x - 4f, RepairHeight);
                EditorGUI.HelpBox(warning, RepairMessage(setting, required), MessageType.Warning);
                var action = required == null ? "Clear Settings" : setting.managedReferenceValue == null ? "Create Settings" : "Replace Settings";
                if (GUI.Button(button, action))
                {
                    SetEntrySettings(entry, required);
                    entry.serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
                rect.y += RepairHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            if (!expanded) return;
            using (new EditorGUI.DisabledScope(disabled.boolValue || repair))
            {
                using var indent = new EditorGUI.IndentLevelScope();
                DevelopmentSettingsFields.ForEachChild(setting, field =>
                {
                    rect.height = EditorGUI.GetPropertyHeight(field, true);
                    EditorGUI.PropertyField(rect, field, true);
                    rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                });
            }
        }

        private void DrawGameEntry(SerializedProperty property)
        {
            var typeName = property.stringValue;
            var selected = string.IsNullOrWhiteSpace(typeName) ? 0 : Array.IndexOf(_gameEntryNames, typeName);
            if (selected < 0)
            {
                EditorGUILayout.HelpBox($"Unavailable game entry: {typeName}", MessageType.Warning);
            }

            var rect = EditorGUILayout.GetControlRect();
            using var scope = new EditorGUI.PropertyScope(rect, new GUIContent(property.displayName, property.tooltip), property);
            EditorGUI.BeginChangeCheck();
            selected = EditorGUI.Popup(rect, scope.content, selected, _gameEntryLabels);
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = _gameEntryNames[selected];
            }
        }
    }
}
