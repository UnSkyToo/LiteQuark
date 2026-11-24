using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class CollectorValidationRunner : EditorValidation
    {
        private const string Root = "Assets/StandaloneAssets/__CollectorReview20261022";
        private const string Outside = "Assets/__CollectorOutside20261022/Shared";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static string ReportRoot => ValidationEnvironment.Reports("collector");

        public static void Begin()
        {
            try
            {
                Directory.CreateDirectory(ReportRoot);
                Run("collector/bundle-graph", BundleGraph);
                Run("collector/cycles", Cycles, "发现循环依赖:");
                Run("collector/flat-collision", FlatBuildCollision);
                Run("collector/settings", ReadOnlySettings);
                Run("collector/viewer", BundleViewerPaths);
                Run("collector/upm-build", UpmBuild.Validate);
                new RegistrationSessions().Begin();
            }
            catch (Exception ex)
            {
                Results.fatalError = ex.ToString();
                Finish();
            }
        }

        private static void Finish()
        {
            Results.Save("collector");
            EditorApplication.Exit(Results.fatalError == null && Results.cases.All(c => c.passed) ? 0 : 2);
        }

        private sealed class RegistrationSessions
        {
            private readonly bool _optionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            private readonly EnterPlayModeOptions _options = EditorSettings.enterPlayModeOptions;
            private readonly SceneSetup[] _scenes = EditorSceneManager.GetSceneManagerSetup();
            private readonly Func<(VersionPackInfo PackInfo, IReadOnlyDictionary<string, string> AssetSourcePaths)> _source = SimulatePackSource.Collect;
            private readonly List<(VersionPackInfo PackInfo, IReadOnlyDictionary<string, string> AssetSourcePaths)> _snapshots = new();
            private double _deadline;
            private double _started;
            private bool _finished;

            internal void Begin()
            {
                Active = new ValidationCase { id = "collector/play-snapshot" };
                Results.cases.Add(Active);
                _started = EditorApplication.timeSinceStartup;
                EditorApplication.playModeStateChanged += OnPlayMode;
                EditorApplication.update += Tick;
                try
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    EditorSettings.enterPlayModeOptionsEnabled = true;
                    EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
                    Enter();
                }
                catch (Exception ex) { Fail(ex); }
            }

            private void Enter()
            {
                _deadline = EditorApplication.timeSinceStartup + 60;
                SimulatePackSource.Collect = null;
                EditorApplication.EnterPlaymode();
            }

            private void OnPlayMode(PlayModeStateChange state)
            {
                try
                {
                    if (state == PlayModeStateChange.EnteredPlayMode)
                    {
                        Check(SimulatePackSource.Collect != null, "Entering Play Mode restores editor registration without Domain Reload.");
                        var task = new LoadSimulateVersionPackTask();
                        try
                        {
                            var waiting = task.Task.GetAwaiter();
                            var published = false;
                            waiting.OnCompleted(() => published = ReferenceEquals(waiting.GetResult(), task.Result) && task.AssetSourcePaths != null);
                            task.Execute();
                            Check(published, "Version await continuation sees the manifest and its source paths.");
                            Check(task.State == TaskState.Succeeded && task.Result is VersionPackInfo pack && pack.IsValid && task.AssetSourcePaths != null,
                                "The real registered collector produces a usable version task result in Play Mode.");
                            _snapshots.Add(((VersionPackInfo)task.Result, task.AssetSourcePaths));
                        }
                        finally { task.Dispose(); }
                        EditorApplication.ExitPlaymode();
                    }
                    else if (state == PlayModeStateChange.EnteredEditMode)
                    {
                        if (Active.failure != null) { End(); return; }
                        if (_snapshots.Count < 2) { Enter(); return; }
                        var first = _snapshots[0];
                        var asset = first.AssetSourcePaths.First();
                        Check(first.PackInfo.TryGetBundleInfoByAssetKey(asset.Key, out _) && AssetDatabase.LoadMainAssetAtPath(asset.Value) != null,
                            "The first snapshot is still usable following the next collection.");
                        Check(!ReferenceEquals(_snapshots[0].PackInfo, _snapshots[1].PackInfo) &&
                            !ReferenceEquals(_snapshots[0].AssetSourcePaths, _snapshots[1].AssetSourcePaths),
                            "Repeated Play Mode creates independent manifest and path snapshots.");
                        Active.passed = true;
                        End();
                    }
                }
                catch (Exception ex) { Fail(ex); }
            }

            private void Tick()
            {
                if (EditorApplication.timeSinceStartup > _deadline)
                {
                    Active.failure = "Play Mode registration validation timed out.";
                    End();
                }
            }

            private void Fail(Exception ex)
            {
                Active.failure = ex.ToString();
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                else End();
            }

            private void End()
            {
                if (_finished) return;
                _finished = true;
                EditorApplication.playModeStateChanged -= OnPlayMode;
                EditorApplication.update -= Tick;
                try
                {
                    EditorSettings.enterPlayModeOptionsEnabled = _optionsEnabled;
                    EditorSettings.enterPlayModeOptions = _options;
                    SimulatePackSource.Collect = _source;
                    if (!EditorApplication.isPlaying && _scenes.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(_scenes);
                }
                catch (Exception ex) { Active.passed = false; Active.failure += "\nCleanup: " + ex; }
                Active.seconds = (float)(EditorApplication.timeSinceStartup - _started);
                Debug.Log("COLLECTOR_REVIEW " + (Active.passed ? "PASS " : "FAIL ") + Active.id);
                Finish();
            }
        }

        private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
        private static object Call(object target, string name, Type[] types, params object[] args) => target.GetType().GetMethod(name, Private, null, types, null).Invoke(target, args);
        private static CollectorValidationAsset Node(string path, params UnityEngine.Object[] references)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.Refresh();
            var asset = AssetDatabase.LoadAssetAtPath<CollectorValidationAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CollectorValidationAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }
            asset.References = references;
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static ResCollector Collect(string folder, ResCollectorSetting setting = null)
        {
            var collector = new ResCollector(setting?.IgnorePathList ?? new List<string>());
            Call(collector, "CollectBundleInfo", new[] { typeof(string) }, folder);
            return collector;
        }
        private static BundleInfo[] Bundles(ResCollector collector) => Field<Dictionary<string, BundleInfo>>(collector, "_bundleInfoCache").Values.ToArray();

        private static void BundleGraph()
        {
            const string ignored = Root + "/Graph/A.v2/Child.v3";
            const string first = ignored + "/MixedCase.asset";
            const string second = "Assets/StandaloneAssets/Assets/__CollectorReview20261022/Other.asset";
            const string dependency = Outside + "/Shared.asset";
            var shared = Node(dependency);
            Node(first, shared);
            Node(second, shared);
            var collector = new ResCollector(new[] { ignored });
            var pack = collector.GetVersionPackInfo("graph", ValidationEnvironment.BundleTarget, false, false);
            pack.Initialize();
            var a = pack.GetBundleInfoFromAssetPath(first);
            var b = pack.GetBundleInfoFromAssetPath(second);
            var material = pack.GetBundleInfoFromAssetPath(dependency);
            Check(a.BundlePath.EndsWith("/a.v2.ab", StringComparison.Ordinal) && a != b,
                "Ignored child joins its dotted parent while the other owner remains separate.");
            Check(b.BundlePath == "assets/__collectorreview20261022.ab" && pack.GetBundleInfoFromAssetPath(AssetPath.ToRootRelativePath(second)) == b,
                "An asset under a nested Assets directory retains its logical Bundle name and can be found using its stored path.");
            Check(a.DependencyList.Contains(material.BundlePath) && b.DependencyList.Contains(material.BundlePath) &&
                pack.BundleList.Count(bundle => bundle.AssetList.Contains(AssetPath.Resolve(dependency))) == 1,
                "Both owners refer to the same indexed dependency.");
            var builder = new ProjectBuilder();
            typeof(ProjectBuilder).GetProperty("Collector").SetValue(builder, collector);
            Call(new ResCollectInfoStep(), "CollectAssetBundleBuilds", new[] { typeof(ProjectBuilder), typeof(VersionPackInfo) }, builder, pack);
            Check(builder.Builds.Single(build => build.assetBundleName == a.BundlePath).assetNames.Contains(first) &&
                collector.GetAssetSourcePath(AssetPath.Resolve(first)) == first, "Build receives the actual mixed-case source path.");
        }

        private static void Cycles()
        {
            const string folder = Root + "/Cycle";
            const string first = folder + "/A.asset";
            const string second = folder + "/B.asset";
            const string moved = folder + "/Child/B.asset";
            var a = Node(first);
            var b = Node(second, a);
            Node(first, b);
            var collector = new ResCollector(Array.Empty<string>());
            try
            {
                var pack = collector.GetVersionPackInfo("cycle", ValidationEnvironment.BundleTarget, false, false);
                pack.Initialize();
                var owner = pack.GetBundleInfoFromAssetPath(first);
                Check(owner == pack.GetBundleInfoFromAssetPath(second) && !owner.DependencyList.Contains(owner.BundlePath),
                    "Two mutually referencing assets in one Bundle produce no self dependency.");
                Directory.CreateDirectory(Path.GetDirectoryName(moved));
                AssetDatabase.Refresh();
                Check(AssetDatabase.MoveAsset(second, moved) == string.Empty, "Move one node to another Bundle preserving its GUID.");
                collector.CleanVersionPackInfo();
                var rejected = false;
                try { collector.GetVersionPackInfo("cycle", ValidationEnvironment.BundleTarget, false, false); }
                catch (InvalidOperationException ex) when (ex.Message.Contains("发现循环依赖")) { rejected = true; }
                Check(rejected, "Splitting the same graph across Bundles rejects its cycle.");
                Node(first);
                var fixedPack = collector.GetVersionPackInfo("cycle", ValidationEnvironment.BundleTarget, false, false);
                fixedPack.Initialize();
                var firstBundle = fixedPack.GetBundleInfoFromAssetPath(first);
                var secondBundle = fixedPack.GetBundleInfoFromAssetPath(moved);
                Check(!firstBundle.DependencyList.Contains(secondBundle.BundlePath) && secondBundle.DependencyList.Contains(firstBundle.BundlePath),
                    "After the cycle is fixed, the same collector returns a manifest with only the remaining dependency between the two Bundles.");
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }

        private static void ReadOnlySettings()
        {
            const string defaultPath = "Assets/Editor/ResSetting.asset";
            const string alternatePath = Root + "/AlternateSetting.asset";
            var saved = new Dictionary<string, byte[]>();
            var settings = AssetDatabase.FindAssets("t:ResCollectorSetting").Select(AssetDatabase.GUIDToAssetPath).ToArray();
            foreach (var path in settings)
            {
                saved.Add(path, File.ReadAllBytes(path));
                saved.Add(path + ".meta", File.ReadAllBytes(path + ".meta"));
            }
            try
            {
                foreach (var path in settings) Check(AssetDatabase.DeleteAsset(path), "Temporarily remove isolated settings asset: " + path);
                var before = AssetDatabase.GetAllAssetPaths().OrderBy(path => path).ToArray();
                Check(ResCollectorSetting.LoadSetting() == null, "Missing configuration is represented by null, without a transient settings object.");
                var buildPack = new ResCollector().GetVersionPackInfo("default", ValidationEnvironment.BundleTarget, false, false);
                var simulate = SimulatePackSource.CreateSnapshot();
                Check(buildPack.BundleList.Length == simulate.PackInfo.BundleList.Length && buildPack.BundleList.Length > 0,
                    "Build and registered simulation collection both work with default rules and no settings asset.");
                Check(before.SequenceEqual(AssetDatabase.GetAllAssetPaths().OrderBy(path => path)) &&
                    !File.Exists(defaultPath) && !File.Exists(defaultPath + ".meta") && ResCollectorSetting.LoadSetting() == null,
                    "Read-only settings lookup and both collection paths create no asset, meta or directory.");

                var created = ResCollectorSetting.CreateSetting();
                created.SetIgnorePath(Root + "/Grouping/Merged/Child", true);
                EditorUtility.SetDirty(created);
                AssetDatabase.SaveAssetIfDirty(created);
                Check(File.Exists(defaultPath) && ReferenceEquals(created, ResCollectorSetting.LoadSetting()) &&
                    ReferenceEquals(created, ResCollectorSetting.CreateSetting()), "Explicit editing creates and reuses one persisted settings asset.");
                Check(AssetDatabase.MoveAsset(defaultPath, alternatePath) == string.Empty, "Move settings to a non-default project path.");
                Check(ReferenceEquals(created, ResCollectorSetting.LoadSetting()), "Read-only lookup still finds settings outside the default path.");
                CollectionRuleTiming(created);
            }
            finally
            {
                AssetDatabase.DeleteAsset(defaultPath);
                AssetDatabase.DeleteAsset(alternatePath);
                foreach (var pair in saved) File.WriteAllBytes(pair.Key, pair.Value);
                AssetDatabase.Refresh();
            }
        }

        private sealed class CollectorRuleBuildCallback : IBuildCallback
        {
            internal ResCollectorSetting Setting;
            internal string Folder;
            internal bool AtProject;
            internal int Changes;

            public CollectorRuleBuildCallback() { }

            public void PreProjectBuild(ProjectBuildConfig config)
            {
                if (AtProject) ChangeRule();
            }

            public void PreStepBuild(ProjectBuildConfig config, Type stepType, string stepName)
            {
                if (!AtProject && stepType == typeof(ResCollectInfoStep)) ChangeRule();
            }

            public void PostStepBuild(ProjectBuildConfig config, Type stepType, string stepName) { }
            public void PostProjectBuild(ProjectBuildConfig config, ProjectBuildReport report) { }

            private void ChangeRule()
            {
                // 自动发现的未配置实例不影响其他构建验证。
                if (Setting == null) return;
                Setting.SetIgnorePath(Folder, true);
                Changes++;
            }
        }

        private static void CollectionRuleTiming(ResCollectorSetting setting)
        {
            const string folder = Root + "/Grouping/Merged/Child";
            const string sourcePath = folder + "/Other.asset";
            Node(sourcePath);
            var mergedBundle = AssetPath.NormalizeBundlePath(AssetPath.ToRootRelativePath(Root + "/Grouping/Merged")) + LiteConst.BundleFileExt;
            var individualBundle = AssetPath.NormalizeBundlePath(AssetPath.ToRootRelativePath(folder)) + LiteConst.BundleFileExt;
            foreach (var atProject in new[] { true, false })
            {
                setting.IgnorePathList.Clear();
                var collector = new ResCollector();
                var callback = new CollectorRuleBuildCallback { Setting = setting, Folder = folder, AtProject = atProject };
                var builder = new ProjectBuilder();
                var config = new ProjectBuildConfig(ValidationEnvironment.BundleTarget, PlayerSettings.bundleVersion,
                    new ResBuildConfig { HashMode = false, FlatMode = false, CopyToStreamingAssets = false },
                    new AppBuildConfig { Enable = false }, null);
                typeof(ProjectBuilder).GetProperty("BuildConfig").SetValue(builder, config);
                typeof(ProjectBuilder).GetProperty("Collector").SetValue(builder, collector);
                typeof(ProjectBuilder).GetField("_buildCallbacks", Private).SetValue(builder, new IBuildCallback[] { callback });
                // 执行真实构建回调和收集步骤，不执行项目设置、文件清理或发布步骤。
                var report = (ProjectBuildReport)Call(builder, "Execute", new[] { typeof(IBuildStep[]) },
                    (object)new IBuildStep[] { new ResCollectInfoStep() });
                var stage = atProject ? "PreProjectBuild" : "PreStepBuild";
                Check(report.IsSuccess && callback.Changes == 1 &&
                    builder.Builds.Single(build => build.assetNames.Contains(sourcePath)).assetBundleName == mergedBundle,
                    stage + " changes rules after Collector construction and affects the actual build mapping.");

                var pack = collector.GetVersionPackInfo(config.Version, config.Target, false, false);
                pack.Initialize();
                setting.IgnorePathList.Clear();
                Check(ReferenceEquals(pack, collector.GetVersionPackInfo(config.Version, config.Target, false, false)) &&
                    pack.GetBundleInfoFromAssetPath(sourcePath).BundlePath == mergedBundle,
                    "Configuration changes do not alter the cached manifest after " + stage + ".");
                collector.CleanVersionPackInfo();
                var refreshed = collector.GetVersionPackInfo(config.Version, config.Target, false, false);
                refreshed.Initialize();
                Check(!ReferenceEquals(pack, refreshed) && refreshed.GetBundleInfoFromAssetPath(sourcePath).BundlePath == individualBundle &&
                    pack.GetBundleInfoFromAssetPath(sourcePath).BundlePath == mergedBundle,
                    "After cleanup, default collection refreshes the rules without changing the previous manifest.");
            }

            var rules = new List<string> { folder };
            var explicitCollector = new ResCollector(rules);
            rules.Clear();
            setting.SetIgnorePath(Root + "/Grouping/Merged", true);
            var explicitPack = explicitCollector.GetVersionPackInfo("explicit", ValidationEnvironment.BundleTarget, false, false);
            explicitPack.Initialize();
            Check(explicitPack.GetBundleInfoFromAssetPath(sourcePath).BundlePath == mergedBundle,
                "Explicit rules are copied on construction and ignore later input and project configuration changes.");
            explicitCollector.CleanVersionPackInfo();
            var explicitRefresh = explicitCollector.GetVersionPackInfo("explicit-next", ValidationEnvironment.BundleTarget, false, false);
            explicitRefresh.Initialize();
            Check(!ReferenceEquals(explicitPack, explicitRefresh) && explicitRefresh.GetBundleInfoFromAssetPath(sourcePath).BundlePath == mergedBundle,
                "Cleaning an explicitly configured Collector preserves its supplied rules.");
        }

        private static void FlatBuildCollision()
        {
            var folder = Root + "/FlatCollision";
            var first = folder + "/a/b_c/First.asset";
            var second = folder + "/a_b/c/Second.asset";
            Node(first); Node(second);
            var collector = Collect(folder);
            var bundles = Bundles(collector);
            Check(bundles.Length == 2 && bundles.Select(b => b.BundlePath).Distinct().Count() == 2, "Actual collector keeps the two logical Bundles separate.");
            var builder = new ProjectBuilder();
            typeof(ProjectBuilder).GetProperty("Collector").SetValue(builder, collector);
            foreach (var hash in new[] { false, true })
            {
                var normal = new VersionPackInfo("collision", "windows", hash, false, bundles);
                Call(new ResCollectInfoStep(), "CollectAssetBundleBuilds", new[] { typeof(ProjectBuilder), typeof(VersionPackInfo) }, builder, normal);
                Check(builder.Builds.Length == 2 && builder.Builds.Select(b => b.assetBundleName).Distinct().Count() == 2, "Non-FlatMode retains distinct build paths, HashMode=" + hash);
                Check(builder.Builds.SelectMany(b => b.assetNames).OrderBy(p => p).SequenceEqual(new[] { first, second }.OrderBy(p => p)), "Normal build source mapping retains both real source paths.");
                var flat = new VersionPackInfo("collision", "windows", hash, true, bundles);
                var target = flat.GetBundleFileBuildPath(bundles[0]);
                Check(target == flat.GetBundleFileBuildPath(bundles[1]), "Golden collision fixture maps both logical names to one physical name.");
                Exception error = null;
                try { Call(new ResCollectInfoStep(), "CollectAssetBundleBuilds", new[] { typeof(ProjectBuilder), typeof(VersionPackInfo) }, builder, flat); }
                catch (TargetInvocationException ex) { error = ex.InnerException; }
                Check(error is InvalidOperationException && error.Message.Contains("Bundle build path collision") &&
                    error.Message.Contains(bundles[0].BundlePath) && error.Message.Contains(bundles[1].BundlePath) && error.Message.Contains(target),
                    "Flat collision reports both logical names and the physical output even with HashMode=" + hash);
            }
            var uniquePath = folder + "/Different/Unique.asset";
            Node(uniquePath);
            var unique = Collect(Path.GetDirectoryName(uniquePath).Replace('\\', '/'));
            typeof(ProjectBuilder).GetProperty("Collector").SetValue(builder, unique);
            var uniquePack = new VersionPackInfo("unique-flat", "windows", false, true, Bundles(unique));
            Call(new ResCollectInfoStep(), "CollectAssetBundleBuilds", new[] { typeof(ProjectBuilder), typeof(VersionPackInfo) }, builder, uniquePack);
            Check(builder.Builds.Length == 1 && builder.Builds.Length == uniquePack.BundleList.Length && builder.Builds.All(b => !b.assetBundleName.Contains("/")), "Non-colliding FlatMode still produces build inputs normally.");
        }

        private static IEnumerable<UnityEditor.IMGUI.Controls.TreeViewItem> FlattenViewer(UnityEditor.IMGUI.Controls.TreeViewItem item)
        {
            yield return item;
            if (item.children == null) yield break;
            foreach (var child in item.children)
            foreach (var descendant in FlattenViewer(child))
                yield return descendant;
        }

        private static void BundleViewerPaths()
        {
            var folder = Root + "/Viewer";
            var outsidePath = Outside + "/ViewerShared.asset";
            var shared = Node(outsidePath);
            var first = folder + "/Group.v2/MixedCase.asset";
            var second = folder + "/Other/Second.asset";
            Node(first, shared); Node(second, shared);
            var collector = Collect(folder);
            var pack = new VersionPackInfo("viewer", "windows", false, false, Bundles(collector));
            var expectedPaths = new[] { first, second, outsidePath };
            var expectedSize = expectedPaths.Sum(path => new FileInfo(path).Length);
            var viewerType = typeof(BundleViewer.BundleViewerTreeView);
            var findAsset = viewerType.GetMethod("FindFirstAssetPath", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var mode in new[] { "BuildWithNormalMode", "BuildWithCombineMode" })
            {
                var tree = new BundleViewer.BundleViewerTreeView(new UnityEditor.IMGUI.Controls.TreeViewState());
                // Keep the constructor's scheduled root scan from collecting unrelated cycle fixtures.
                EditorApplication.delayCall -= tree.Reload;
                viewerType.GetField("_collector", Private).SetValue(tree, collector);
                var roots = (List<UnityEditor.IMGUI.Controls.TreeViewItem>)Call(tree, mode, new[] { typeof(VersionPackInfo) }, pack);
                var nodes = roots.SelectMany(FlattenViewer).Cast<BundleViewer.BundleViewerTreeItem>().ToArray();
                var assets = nodes.Where(n => n.Type != "bundle").ToArray();
                Check(assets.Select(n => n.Path).OrderBy(p => p).SequenceEqual(expectedPaths.OrderBy(p => p)), mode + ": source casing and root-external paths are retained exactly.");
                Check(assets.All(n => n.Size == new FileInfo(n.Path).Length), mode + ": resource size is read from the real source file.");
                Check(roots.Cast<BundleViewer.BundleViewerTreeItem>().Sum(n => n.Size) == expectedSize, mode + ": aggregate size counts every resource once.");
                foreach (var bundle in pack.BundleList)
                {
                    var item = tree.GetDependencyTreeItem(bundle.BundlePath);
                    Check(item != null && item.Path == bundle.BundlePath && item.Type == "bundle", mode + ": Bundle row retains logical name " + bundle.BundlePath);
                    Check(item.DependencyList.SequenceEqual(bundle.DependencyList), mode + ": dependency links retain logical Bundle keys.");
                }
                foreach (var node in nodes.Where(n => n.Type == "bundle"))
                {
                    var target = (string)findAsset.Invoke(null, new object[] { node });
                    var firstAsset = FlattenViewer(node).Cast<BundleViewer.BundleViewerTreeItem>().First(n => n.Type != "bundle");
                    Check(target == firstAsset.Path && expectedPaths.Contains(target), mode + ": Alt target resolves to the first real resource under " + node.Path);
                    Check(AssetDatabase.IsValidFolder(PathUtils.GetParentPath(target)), mode + ": resolved Alt parent is an existing Unity project folder.");
                    Check(node.Size == FlattenViewer(node).Cast<BundleViewer.BundleViewerTreeItem>().Where(n => n.Type != "bundle").Sum(n => n.Size), mode + ": intermediate folder size is correct.");
                }
                Check((string)findAsset.Invoke(null, new object[] { assets[0] }) == assets[0].Path, mode + ": resource row targeting preserves the real path.");
                Check(findAsset.Invoke(null, new object[] { new BundleViewer.BundleViewerTreeItem { Type = "bundle", Path = "empty.ab" } }) == null, mode + ": empty virtual Bundle node has no target.");
            }
        }

    }
}
