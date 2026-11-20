using System;
using System.Collections.Generic;
using System.Linq;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Editor
{
    internal sealed class LauncherSettingsValidation : EditorValidation
    {
        internal static ValidationCase RunInEditMode()
        {
            Run("tasks/launcher-settings", Exercise);
            return Results.cases.Last();
        }

        private static void Exercise()
        {
            var path = "Assets/LauncherSettings_" + Guid.NewGuid().ToString("N") + ".unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var root = new GameObject("Launcher");
                SceneManager.MoveGameObjectToScene(root, scene);
                var launcher = root.AddComponent<LiteLauncher>();
                var camera = root.AddComponent<Camera>();
                using (var fields = new SerializedObject(launcher))
                {
                    var list = fields.FindProperty("Settings.SystemList");
                    LiteLauncherEditor.AddSystem(list, typeof(UISystem));
                    LiteLauncherEditor.AddSystem(list, typeof(AudioSystem));
                    var ui = list.GetArrayElementAtIndex(0);
                    var data = ui.FindPropertyRelative(nameof(LiteSystemEntry.Setting));
                    Check(data.managedReferenceValue is UISystemSetting &&
                        list.GetArrayElementAtIndex(1).FindPropertyRelative(nameof(LiteSystemEntry.Setting)).managedReferenceValue == null,
                        "Adding UI creates settings on its own registration; Audio needs no settings.");
                    data.FindPropertyRelative(nameof(UISystemSetting.UICamera)).objectReferenceValue = camera;
                    data.FindPropertyRelative(nameof(UISystemSetting.ResolutionWidth)).intValue = 1234;
                    ui.FindPropertyRelative(nameof(LiteSystemEntry.Disabled)).boolValue = true;
                    list.MoveArrayElement(0, 1);
                    var cdn = fields.FindProperty("Settings.Asset.BundleRemoteUriByBuild");
                    cdn.FindPropertyRelative("Release").stringValue = "https://release.example/";
                    cdn.FindPropertyRelative("OverrideInDevelopment").boolValue = false;
                    var inheritedCdn = BuildValueDrawer.ValueFor(cdn, LiteBuildProfile.Development, out var writable);
                    Check(!writable && inheritedCdn.stringValue == "https://release.example/",
                        "With Override off, the Development input displays the current Release value read-only.");
                    cdn.FindPropertyRelative("OverrideInDevelopment").boolValue = true;
                    var devCdn = BuildValueDrawer.ValueFor(cdn, LiteBuildProfile.Development, out writable);
                    Check(writable && devCdn.name == "Development", "Override enables only the Development input.");
                    devCdn.stringValue = string.Empty;
                    var releaseCdn = BuildValueDrawer.ValueFor(cdn, LiteBuildProfile.Release, out writable);
                    Check(writable && releaseCdn.name == "Release" && releaseCdn.stringValue == "https://release.example/",
                        "The Release view still edits its own value even when the Development override is active.");
                    var log = fields.FindProperty("Settings.Log.ShowLogViewerByBuild");
                    log.FindPropertyRelative("Release").boolValue = true;
                    log.FindPropertyRelative("OverrideInDevelopment").boolValue = true;
                    log.FindPropertyRelative("Development").boolValue = false;
                    fields.ApplyModifiedProperties();
                }
                if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Could not save the settings scene.");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                launcher = scene.GetRootGameObjects().Single().GetComponent<LiteLauncher>();
                var registrations = launcher.Settings.SystemList;
                var authoredUI = (UISystemSetting)registrations[1].Setting;
                Check(registrations[0].AssemblyQualifiedName == typeof(AudioSystem).AssemblyQualifiedName && registrations[1].Disabled &&
                    authoredUI.ResolutionWidth == 1234 && authoredUI.UICamera == launcher.GetComponent<Camera>(),
                    "Reordering and disabling survive a scene save and reload together with the UI parameters and scene camera.");
                using (var fields = new SerializedObject(launcher))
                {
                    Undo.IncrementCurrentGroup();
                    var disabled = fields.FindProperty("Settings.SystemList").GetArrayElementAtIndex(1)
                        .FindPropertyRelative(nameof(LiteSystemEntry.Disabled));
                    disabled.boolValue = false;
                    fields.ApplyModifiedProperties();
                    Undo.FlushUndoRecordObjects();
                    Undo.PerformUndo();
                    Check(launcher.Settings.SystemList[1].Disabled, "Undo restores the registration's disabled state.");
                    Undo.PerformRedo();
                }
                authoredUI = (UISystemSetting)launcher.Settings.SystemList[1].Setting;
                launcher.EditorPreview = LiteBuildProfile.Development;
                var development = launcher.CreateRuntimeSetting();
                launcher.EditorPreview = LiteBuildProfile.Release;
                var release = launcher.CreateRuntimeSetting();
                Check(development.Asset.BundleRemoteUri == string.Empty && !development.Log.ShowLogViewer &&
                    release.Asset.BundleRemoteUri == "https://release.example/" && release.Log.ShowLogViewer,
                    "Empty and false Development overrides survive serialization without changing Release.");
                launcher.Settings.Asset.BundleRemoteUriByBuild.OverrideInDevelopment = false;
                launcher.Settings.Asset.BundleRemoteUri = "https://updated.example/";
                var inherited = launcher.Settings.CreateRuntimeSetting(LiteBuildProfile.Development);
                Check(inherited.Asset.BundleRemoteUri == "https://updated.example/" && development.Asset.BundleRemoteUri == string.Empty,
                    "Clearing the override follows the latest Release value while already resolved settings stay independent.");
                var runtimeUI = (UISystemSetting)development.SystemList[1].Setting;
                runtimeUI.ResolutionWidth = 42;
                Check(!development.SystemList[1].Disabled && runtimeUI.UICamera == authoredUI.UICamera && authoredUI.ResolutionWidth == 1234,
                    "A re-enabled registration owns an independent runtime setting with the same scene camera.");
                CheckRegistrationRepair(launcher);
                CheckDevelopmentFields(launcher);
                CheckCdnWarnings();
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void CheckRegistrationRepair(LiteLauncher launcher)
        {
            using var fields = new SerializedObject(launcher);
            var list = fields.FindProperty("Settings.SystemList");
            var entry = list.GetArrayElementAtIndex(1);
            var setting = entry.FindPropertyRelative(nameof(LiteSystemEntry.Setting));
            Check(!LiteLauncherEditor.NeedsSettingsRepair(entry, out _) &&
                !LiteLauncherEditor.NeedsSettingsRepair(list.GetArrayElementAtIndex(0), out _),
                "Valid UI settings and a stateless Audio system require no repair.");
            entry.FindPropertyRelative(nameof(LiteSystemEntry.Disabled)).boolValue = true;
            setting.managedReferenceValue = null;
            fields.ApplyModifiedProperties();
            Check(LiteLauncherEditor.NeedsSettingsRepair(entry, out var required) && required == typeof(UISystemSetting) &&
                setting.managedReferenceValue == null, "A registered system now requiring settings offers in-place creation.");
            LiteLauncherEditor.SetEntrySettings(entry, required);
            fields.ApplyModifiedProperties();
            Check(setting.managedReferenceValue is UISystemSetting, "Creating missing UI settings uses the provider contract.");

            setting.managedReferenceValue = new NetworkSystemSetting { DefaultTimeout = 37 };
            fields.ApplyModifiedProperties();
            Check(LiteLauncherEditor.NeedsSettingsRepair(entry, out required) && required == typeof(UISystemSetting) &&
                ((NetworkSystemSetting)setting.managedReferenceValue).DefaultTimeout == 37,
                "Detecting an incompatible registration retains its existing data until the user requests replacement.");
            Undo.IncrementCurrentGroup();
            LiteLauncherEditor.SetEntrySettings(entry, required);
            fields.ApplyModifiedProperties();
            Undo.FlushUndoRecordObjects();
            var repaired = launcher.Settings.SystemList[1];
            Check(repaired.Disabled && repaired.AssemblyQualifiedName == typeof(UISystem).AssemblyQualifiedName &&
                repaired.Setting is UISystemSetting && launcher.Settings.SystemList[0].Setting == null,
                "Replacement changes the settings only, preserving order, the selected system and the disabled state.");
            Undo.PerformUndo();
            Check(launcher.Settings.SystemList[1].Setting is NetworkSystemSetting old && old.DefaultTimeout == 37,
                "Undo restores the incompatible settings and their values.");
        }

        private static class ThirdParty
        {
            [Serializable]
            internal struct BuildValue<T> { public T Ordinary; }
        }

        [Serializable]
        private sealed class Options
        {
            public BuildValue<int> Limit = new(5, 7);
            public int Shared = 9;
        }

        [Serializable]
        private sealed class ConditionalSettings : ISystemSetting
        {
            public bool Show = true;
            public List<Options> PlainList = new() { new Options() };
            public Options[] PlainArray = { new Options() };
            [ConditionalShow(nameof(Show), true)] public Options Group = new();
            [ConditionalShow(nameof(Show), true)] public List<Options> Groups = new() { new Options() };
            public ThirdParty.BuildValue<int> Foreign = new() { Ordinary = 3 };
        }

        private static void CheckDevelopmentFields(LiteLauncher launcher)
        {
            var config = new ConditionalSettings();
            launcher.Settings.SystemList.Add(new LiteSystemEntry { Setting = config });
            using var serialized = new SerializedObject(launcher);
            var list = serialized.FindProperty("Settings.SystemList");
            var root = list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative(nameof(LiteSystemEntry.Setting));
            var visible = new Dictionary<string, DevelopmentSettingsFields.Access>();
            bool Inspect(SerializedProperty property, System.Reflection.FieldInfo field, DevelopmentSettingsFields.Access access, int depth)
            {
                visible.Add(property.propertyPath.Substring(root.propertyPath.Length + 1), access);
                return property.hasVisibleChildren;
            }

            DevelopmentSettingsFields.Visit(root, null, Inspect);
            Check(visible["Foreign"] == DevelopmentSettingsFields.Access.Inherited &&
                visible["Foreign.Ordinary"] == DevelopmentSettingsFields.Access.Inherited,
                "Another namespace's BuildValue<T> and its ordinary data stay read-only in Development.");
            Check(visible["Group"] == DevelopmentSettingsFields.Access.Foldout &&
                visible["Group.Limit"] == DevelopmentSettingsFields.Access.Override &&
                visible["Group.Shared"] == DevelopmentSettingsFields.Access.Inherited,
                "The visible conditional object exposes its override and keeps its ordinary field read-only.");
            foreach (var name in new[] { nameof(ConditionalSettings.PlainList), nameof(ConditionalSettings.PlainArray), nameof(ConditionalSettings.Groups) })
            {
                var element = name + ".Array.data[0]";
                Check(visible[name] == DevelopmentSettingsFields.Access.Foldout &&
                    visible[element + ".Limit"] == DevelopmentSettingsFields.Access.Override &&
                    visible[element + ".Shared"] == DevelopmentSettingsFields.Access.Inherited,
                    $"{name} has a foldout, and its element exposes an override for Limit and a read-only Shared field.");
            }
            Check(visible["Groups.Array.data[0]"] == DevelopmentSettingsFields.Access.Foldout,
                "The conditional list element has its own foldout for visiting its fields.");
            root.FindPropertyRelative("Show").boolValue = false;
            visible.Clear();
            DevelopmentSettingsFields.Visit(root, null, Inspect);
            Check(!visible.Keys.Any(path => path.StartsWith("Group", StringComparison.Ordinal)) &&
                visible.ContainsKey("PlainList.Array.data[0].Limit") && visible.ContainsKey("PlainArray.Array.data[0].Limit"),
                "Switching the condition off hides both conditional containers and leaves the plain list and array visible.");
        }

        private static void CheckCdnWarnings()
        {
            var asset = new LiteSetting.AssetSetting
            {
                BundleModeByBuild = new BuildValue<BundleLoadMode>(BundleLoadMode.BuiltIn, BundleLoadMode.Remote),
                BundleRemoteUriByBuild = new BuildValue<string>("https://cdn.example/content/", "")
            };
            Check(!LiteLauncherEditor.NeedsCdnWarning(asset, LiteBuildProfile.Release) &&
                LiteLauncherEditor.NeedsCdnWarning(asset, LiteBuildProfile.Development),
                "Only the effective Remote environment warns about an empty CDN override.");
            asset.BundleRemoteUriByBuild.OverrideInDevelopment = false;
            Check(!LiteLauncherEditor.NeedsCdnWarning(asset, LiteBuildProfile.Development),
                "A Development Remote source can inherit a valid Release CDN.");
            asset.BundleModeByBuild = new BuildValue<BundleLoadMode>(BundleLoadMode.Remote, BundleLoadMode.Simulate);
            asset.BundleRemoteUri = "relative/content";
            Check(LiteLauncherEditor.NeedsCdnWarning(asset, LiteBuildProfile.Release) &&
                !LiteLauncherEditor.NeedsCdnWarning(asset, LiteBuildProfile.Development),
                "A relative CDN warns for Release Remote; a Development Simulate override ignores it.");
            asset.BundleRemoteUri = "http://[";
            Check(LiteLauncherEditor.NeedsCdnWarning(asset, LiteBuildProfile.Release), "Malformed absolute CDN addresses also warn.");
        }
    }
}
