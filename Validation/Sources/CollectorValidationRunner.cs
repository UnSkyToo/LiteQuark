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
    internal static class CollectorValidationRunner
    {
        private const string Root = "Assets/StandaloneAssets/__CollectorReview20261022";
        private const string Outside = "Assets/__CollectorOutside20261022/Shared";
        private const string RootFile = "Assets/StandaloneAssets/__CollectorRoot20261022.asset";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Report Results = new();
        private static Case Active;
        private static string ReportRoot => ValidationEnvironment.Reports("collector");
        [Serializable] private sealed class Report { public string unityVersion; public List<Case> cases = new(); public string fatalError; }
        [Serializable] private sealed class Case { public string name; public bool passed; public int checks; public string failure; public List<string> evidence = new(); }

        public static void Begin()
        {
            try
            {
                Directory.CreateDirectory(ReportRoot);
                Run("01 Existing folder and IgnorePathList grouping", Grouping);
                Run("02 Multiple external dependencies in one Bundle", ExternalDependencies);
                Run("03 Shared dependency collected once", SharedDependency);
                Run("04 Root dependency belongs to default.ab", RootDependency);
                Run("05 Build inputs preserve real source casing", BuildInputs);
                Run("06 Resource cycle within one Bundle terminates", InternalCycle);
                Run("07 Cleanup resets source and dependency caches", Reset);
                Run("08 Canonical path collision rejected", Collision);
                Run("09 Full project paths and dependency ID round trip", RoundTrip);
                Run("10 Cross-Bundle cycle rejected before caching", CrossCycle);
                Run("11 Dotted directories retain their own Bundle and dependency edge", DottedDirectory);
                Run("12 Dotted ignored folders retain configured merging", DottedIgnored);
                Run("13 Ordinary packages directory collection and mapping", PackagesDirectory);
                Run("14 BundleViewer builds both modes from real source paths", BundleViewerPaths);
                Run("15 FlatMode build path collisions fail before Bundle build", FlatBuildCollision);
                Run("16 Collection reads settings without creating assets", ReadOnlySettings);
                Run("17 Simulate snapshot survives collector cleanup and task delivery", SimulateSnapshot);
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
            Results.unityVersion = Application.unityVersion;
            File.WriteAllText(Path.Combine(ReportRoot, "results.json"), JsonUtility.ToJson(Results, true));
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
            private bool _finished;

            internal void Begin()
            {
                Active = new Case { name = "18 Simulate registration survives repeated Play Mode without Domain Reload" };
                Results.cases.Add(Active);
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
                            task.Execute();
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
                Debug.Log("COLLECTOR_REVIEW " + (Active.passed ? "PASS " : "FAIL ") + Active.name);
                Finish();
            }
        }
        private static void Run(string name, Action test)
        {
            Active = new Case { name = name };
            Results.cases.Add(Active);
            try { test(); Active.passed = true; }
            catch (Exception ex) { Active.failure = ex.ToString(); }
            Debug.Log("COLLECTOR_REVIEW " + (Active.passed ? "PASS " : "FAIL ") + name);
        }
        private static void Check(bool value, string evidence)
        {
            Active.checks++;
            if (!value) throw new Exception(evidence);
            Active.evidence.Add(evidence);
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
        private static ResCollectorSetting Setting(params string[] ignored)
        {
            var result = ScriptableObject.CreateInstance<ResCollectorSetting>();
            result.IgnorePathList.AddRange(ignored);
            return result;
        }
        private static ResCollector Collect(string folder, ResCollectorSetting setting = null)
        {
            var collector = new ResCollector(setting?.IgnorePathList ?? new List<string>());
            Call(collector, "CollectBundleInfo", new[] { typeof(string) }, folder);
            return collector;
        }
        private static BundleInfo[] Bundles(ResCollector collector) => Field<Dictionary<string, BundleInfo>>(collector, "_bundleInfoCache").Values.ToArray();
        private static BundleInfo Owner(ResCollector collector, string path) => Bundles(collector).Single(b => b.AssetList.Any(a => AssetPath.Resolve(a) == AssetPath.Resolve(path)));
        private static string BundlePath(object collector, string path) => (string)Call(collector, "GetBundlePathFromFullPath", new[] { typeof(string) }, path);

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

        private static void SimulateSnapshot()
        {
            var path = Root + "/Snapshot/MixedCase.asset";
            var asset = Node(path);
            var collector = new ResCollector(Array.Empty<string>());
            var snapshot = (PackInfo: collector.GetVersionPackInfo("snapshot", ValidationEnvironment.BundleTarget, false, false),
                AssetSourcePaths: collector.GetAssetSourcePathsSnapshot());
            var key = AssetPath.Resolve(path);
            var sources = Field<Dictionary<string, string>>(collector, "_assetSourcePathMap");
            sources[key] = "Assets/Wrong.asset";
            collector.CleanVersionPackInfo();
            snapshot.PackInfo.Initialize();
            var info = snapshot.PackInfo.GetBundleInfoFromAssetPath(path);
            Check(snapshot.AssetSourcePaths[key] == path && info != null,
                "Manifest and mixed-case source mapping survive changes to the collector dictionary and collector cleanup.");
            var immutable = false;
            try { ((IDictionary<string, string>)snapshot.AssetSourcePaths)[key] = "Assets/Wrong.asset"; }
            catch (NotSupportedException) { immutable = true; }
            Check(immutable, "Consumers cannot modify the published source mapping.");
            var next = (PackInfo: collector.GetVersionPackInfo("next", ValidationEnvironment.BundleTarget, false, false),
                AssetSourcePaths: collector.GetAssetSourcePathsSnapshot());
            Check(!ReferenceEquals(snapshot.PackInfo, next.PackInfo) && !ReferenceEquals(snapshot.AssetSourcePaths, next.AssetSourcePaths) &&
                snapshot.PackInfo.Version == "snapshot", "Recollection creates independent manifest and source snapshots.");

            var original = SimulatePackSource.Collect;
            LoadSimulateVersionPackTask versionTask = null;
            LoadSimulateBundleTask bundleTask = null;
            BaseTask assetTask = null;
            IBundleResource resource = null;
            try
            {
                var collections = 0;
                SimulatePackSource.Collect = () => { collections++; return snapshot; };
                var continuationSawData = false;
                versionTask = new LoadSimulateVersionPackTask();
                var awaiter = versionTask.Task.GetAwaiter();
                awaiter.OnCompleted(() => continuationSawData =
                    ReferenceEquals(awaiter.GetResult(), versionTask.Result) && ReferenceEquals(versionTask.AssetSourcePaths, snapshot.AssetSourcePaths));
                Check(collections == 0 && versionTask.AssetSourcePaths == null, "Task construction does not collect or publish data.");
                versionTask.Execute();
                Check(collections == 1 && continuationSawData && ReferenceEquals(versionTask.Result, snapshot.PackInfo),
                    "Version execution publishes the snapshot before completion and keeps VersionPackInfo as its Result.");
                bundleTask = new LoadSimulateBundleTask(info, versionTask.AssetSourcePaths);
                bundleTask.Execute();
                bundleTask.Tick(0);
                resource = bundleTask.GetBundle();
                assetTask = resource.CreateLoadAssetTask<CollectorValidationAsset>(key, null);
                assetTask.Execute();
                assetTask.Tick(0);
                Check(assetTask.State == TaskState.Succeeded && ReferenceEquals(assetTask.Result, asset),
                    "Simulation loads the real mixed-case source using only the data snapshot after Collector cleanup.");
                SimulatePackSource.Collect = null;
                var rejected = false;
                try { SimulatePackSource.CreateSnapshot(); }
                catch (InvalidOperationException ex) when (ex.Message.Contains("not registered")) { rejected = true; }
                Check(rejected, "An absent editor registration reports a clear failure instead of silently using another backend.");
                var incompleteSnapshots = new[]
                {
                    (PackInfo: (VersionPackInfo)null, snapshot.AssetSourcePaths),
                    (snapshot.PackInfo, AssetSourcePaths: (IReadOnlyDictionary<string, string>)null)
                };
                foreach (var incomplete in incompleteSnapshots)
                {
                    SimulatePackSource.Collect = () => incomplete;
                    rejected = false;
                    try { SimulatePackSource.CreateSnapshot(); }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("incomplete snapshot")) { rejected = true; }
                    Check(rejected, "An incomplete tuple is rejected before manifest or source data can be published.");
                }
                Check(typeof(ResCollector).Assembly.GetName().Name == "LiteQuark.Editor" &&
                    typeof(ResCollectorSetting).Assembly.GetName().Name == "LiteQuark.Editor" &&
                    typeof(SimulateBundleProvider).Assembly == typeof(VersionPackInfo).Assembly &&
                    !typeof(VersionPackInfo).Assembly.GetTypes().Any(type => type.Namespace == "LiteQuark.Editor"),
                    "Collector and settings belong to Editor; Simulate remains in Runtime without Editor namespace types.");
            }
            finally
            {
                SimulatePackSource.Collect = original;
                assetTask?.Dispose();
                resource?.Dispose();
                bundleTask?.Dispose();
                versionTask?.Dispose();
                AssetDatabase.DeleteAsset(Root + "/Snapshot");
            }
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
            var unique = Collect(Root + "/Grouping");
            typeof(ProjectBuilder).GetProperty("Collector").SetValue(builder, unique);
            var uniquePack = new VersionPackInfo("unique-flat", "windows", false, true, Bundles(unique));
            Call(new ResCollectInfoStep(), "CollectAssetBundleBuilds", new[] { typeof(ProjectBuilder), typeof(VersionPackInfo) }, builder, uniquePack);
            Check(builder.Builds.Length == uniquePack.BundleList.Length && builder.Builds.All(b => !b.assetBundleName.Contains("/")), "Non-colliding FlatMode still produces build inputs normally.");
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


        private static void AssertGraph(ResCollector collector, params (string Path, string[] Assets, string[] Dependencies)[] expected)
        {
            var actual = Bundles(collector);
            Check(actual.Length == expected.Length, "Exact Bundle count: " + expected.Length);
            Check(actual.Select(b => b.BundleID).OrderBy(id => id).SequenceEqual(Enumerable.Range(1, expected.Length)), "Bundle IDs are unique and contiguous from one.");
            foreach (var item in expected)
            {
                var bundle = actual.Single(b => b.BundlePath == item.Path);
                Check(bundle.AssetList.OrderBy(a => a).SequenceEqual(item.Assets.Select(a => a.ToLowerInvariant()).OrderBy(a => a)), "Exact full-path membership: " + item.Path);
                Check(bundle.DependencyList.OrderBy(d => d).SequenceEqual(item.Dependencies.OrderBy(d => d)), "Exact dependency graph: " + item.Path);
            }
        }

        private static void PackagesDirectory()
        {
            const string path = "Assets/StandaloneAssets/packages/__validation/Probe.txt";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, "ordinary-packages-folder");
                AssetDatabase.Refresh();
                var collector = Collect(Path.GetDirectoryName(path).Replace('\\', '/'));
                var key = path.ToLowerInvariant();
                Check(AssetDatabase.LoadAssetAtPath<TextAsset>(path).text == "ordinary-packages-folder", "Editor loads the ordinary packages resource.");
                Check(Bundles(collector).Single().AssetList.Single() == key && collector.GetAssetSourcePath(key) == path, "Collection retains full identity and real source casing.");
                foreach (var alias in new[] { "packages/__validation/Probe.txt", "Packages/__validation/Probe.txt", "packages\\__validation\\Probe.txt", path })
                    Check(AssetPath.Resolve(alias) == key, "Ordinary packages alias: " + alias);
                var rejected = false;
                try { collector.GetAssetSourcePath("packages/__validation/Probe.txt"); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "Internal source mapping requires a full project path.");
                var pack = new VersionPackInfo("validation", "windows", false, false, Bundles(collector));
                var builder = new ProjectBuilder();
                typeof(ProjectBuilder).GetProperty("Collector").SetValue(builder, collector);
                Call(new ResCollectInfoStep(), "CollectAssetBundleBuilds", new[] { typeof(ProjectBuilder), typeof(VersionPackInfo) }, builder, pack);
                Check(builder.Builds.Single().assetNames.Single() == path, "Production build mapping resolves the real packages source.");
                var restored = VersionPackInfo.FromBinaryData(pack.ToBinaryData());
                restored.Initialize();
                Check(restored.GetBundleInfoFromAssetPath(path) != null, "Full packages identity survives manifest round trip.");
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }

        private static void Grouping()
        {
            var existingSetting = AssetDatabase.LoadAssetAtPath<ResCollectorSetting>("Assets/Editor/ResSetting.asset");
            Check(existingSetting != null && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(existingSetting))) == "5651dc22344e4fab9c65f8b493246dc0",
                "The existing serialized settings asset resolves the moved script with its original GUID.");
            var inspector = UnityEditor.Editor.CreateEditor(existingSetting);
            try
            {
                Check(inspector is ResCollectorSettingEditor, "The settings asset still uses its existing custom Inspector after the assembly move.");
            }
            finally { UnityEngine.Object.DestroyImmediate(inspector); }

            var folder = Root + "/Grouping";
            var plain = folder + "/Plain/Upper.asset";
            var child = folder + "/Merged/Child/Other.asset";
            Node(plain); Node(child);
            var setting = Setting(folder + "/Merged/Child");
            var collector = Collect(folder, setting);

            AssertGraph(collector,
                ("__collectorreview20261022/grouping/plain.ab", new[] { plain }, Array.Empty<string>()),
                ("__collectorreview20261022/grouping/merged.ab", new[] { child }, Array.Empty<string>()));
            Check(Owner(collector, plain).BundlePath == "__collectorreview20261022/grouping/plain.ab", "Ordinary folder remains an independent Bundle.");
            Check(Owner(collector, child).BundlePath == "__collectorreview20261022/grouping/merged.ab", "Ignored child folder still merges into its parent.");
        }
        private static void ExternalDependencies()
        {
            var first = Node(Outside + "/First.asset");
            var second = Node(Outside + "/Second.asset");
            var owner = Root + "/External/Owner.asset";
            Node(owner, first, second);
            var collector = Collect(Root + "/External");

            AssertGraph(collector,
                ("__collectorreview20261022/external.ab", new[] { owner }, new[] { "assets/__collectoroutside20261022/shared.ab" }),
                ("assets/__collectoroutside20261022/shared.ab", new[] { Outside + "/First.asset", Outside + "/Second.asset" }, Array.Empty<string>()));
            var firstBundle = Owner(collector, Outside + "/First.asset");
            Check(firstBundle == Owner(collector, Outside + "/Second.asset") && firstBundle.AssetList.Length == 2, "Both root-external assets in the same Bundle are collected.");
            Check(Owner(collector, owner).DependencyList.SequenceEqual(new[] { firstBundle.BundlePath }), "The owner retains only one dependency edge for the shared Bundle.");
            Check(firstBundle.AssetList.All(a => a.StartsWith("assets/", StringComparison.Ordinal)), "External entries remain lowercase project paths.");
            Check(collector.GetAssetSourcePath(Outside.ToLowerInvariant() + "/first.asset") == Outside + "/First.asset", "External source path preserves real casing.");
        }
        private static void SharedDependency()
        {
            var shared = Node(Outside + "/Shared.asset");
            Node(Root + "/Shared/A/Owner.asset", shared);
            Node(Root + "/Shared/B/Owner.asset", shared);
            var collector = Collect(Root + "/Shared");

            AssertGraph(collector,
                ("__collectorreview20261022/shared/a.ab", new[] { Root + "/Shared/A/Owner.asset" }, new[] { "assets/__collectoroutside20261022/shared.ab" }),
                ("__collectorreview20261022/shared/b.ab", new[] { Root + "/Shared/B/Owner.asset" }, new[] { "assets/__collectoroutside20261022/shared.ab" }),
                ("assets/__collectoroutside20261022/shared.ab", new[] { Outside + "/Shared.asset" }, Array.Empty<string>()));
            var key = AssetPath.Resolve(Outside + "/Shared.asset");
            Check(Bundles(collector).SelectMany(b => b.AssetList).Count(a => AssetPath.Resolve(a) == key) == 1, "A dependency reached from two owners appears once.");
            Check(Field<Dictionary<string, string[]>>(collector, "_dependencyCache").Keys.All(k => k.StartsWith("Assets/", StringComparison.Ordinal)), "Every AssetDatabase dependency query used a real project path.");
        }
        private static void RootDependency()
        {
            var root = Node(RootFile);
            var owner = Root + "/RootReference/Owner.asset";
            Node(owner, root);
            var collector = Collect(Root + "/RootReference");

            AssertGraph(collector,
                ("__collectorreview20261022/rootreference.ab", new[] { owner }, new[] { "default.ab" }),
                ("default.ab", new[] { RootFile }, Array.Empty<string>()));
            Check(Owner(collector, RootFile).BundlePath == "default.ab", "A root-level file collected through a dependency belongs to default.ab.");
            Check(!Bundles(collector).Any(b => b.BundlePath == ".ab"), "No empty-stem .ab Bundle is produced.");
            Call(collector, "CreateBundleInfo", new[] { typeof(string), typeof(string[]) }, LiteConst.AssetRootPath, new[] { RootFile });
            Check(Bundles(collector).SelectMany(b => b.AssetList).Count(a => AssetPath.Resolve(a) == AssetPath.Resolve(RootFile)) == 1, "Later root directory collection does not duplicate the resource.");
        }
        private static void BuildInputs()
        {
            var path = Root + "/BuildInput/MixedCase.asset";
            Node(path);
            var collector = Collect(Root + "/BuildInput");
            var pack = new VersionPackInfo("test", "windows", false, false, Bundles(collector));
            var builder = new ProjectBuilder();
            typeof(ProjectBuilder).GetProperty("Collector").SetValue(builder, collector);
            Call(new ResCollectInfoStep(), "CollectAssetBundleBuilds", new[] { typeof(ProjectBuilder), typeof(VersionPackInfo) }, builder, pack);
            Check(builder.Builds.Single().assetNames.Single() == path, "AssetBundleBuild.assetNames contains the actual mixed-case source path.");
            Check(builder.Builds.Single().assetBundleName == pack.BundleList.Single().BundlePath, "AssetBundleBuild keeps the collected Bundle name.");
            Check(collector.GetAssetSourcePath(AssetPath.Resolve(path)) == path, "Canonical lookup returns the real source path.");
        }
        private static void InternalCycle()
        {
            var folder = Root + "/InternalCycle";
            var a = Node(folder + "/A.asset"); var b = Node(folder + "/B.asset", a);
            a.References = new UnityEngine.Object[] { b }; EditorUtility.SetDirty(a); AssetDatabase.SaveAssets();
            var collector = Collect(folder);
            Check(Bundles(collector).Length == 1 && Bundles(collector)[0].AssetList.Length == 2, "A resource cycle in one Bundle terminates and collects both nodes.");
            Check(Bundles(collector)[0].DependencyList.Length == 0, "A resource cycle does not create a Bundle self-dependency.");
        }
        private static void Reset()
        {
            var path = Root + "/Reset/Item.asset";
            Node(path);
            var collector = Collect(Root + "/Reset");
            collector.CleanVersionPackInfo();
            Check(Field<Dictionary<string, string>>(collector, "_assetSourcePathMap").Count == 0 && Bundles(collector).Length == 0 && Field<Dictionary<string, string[]>>(collector, "_dependencyCache").Count == 0, "Cleanup empties all collection dictionaries.");
            Check(Field<int>(collector, "_bundleID") == 1 && Field<VersionPackInfo>(collector, "_packInfo") == null, "Cleanup resets numbering and cached pack.");
            Call(collector, "CollectBundleInfo", new[] { typeof(string) }, Root + "/Reset");
            Check(collector.GetAssetSourcePath(path) == path && Bundles(collector).Length == 1, "Collector can collect again after cleanup.");
        }
        private static void Collision()
        {
            var collector = new ResCollector();
            var method = typeof(ResCollector).GetMethod("TryRegisterAssetSourcePath", Private);
            Check((bool)method.Invoke(collector, new object[] { "Assets/Example/Mixed.asset", null }), "First source path registers.");
            var rejected = false;
            try { method.Invoke(collector, new object[] { "Assets/Example/mixed.asset", null }); }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { rejected = true; }
            Check(rejected, "Different source casing with the same canonical key is rejected.");
        }
        private static void RoundTrip()
        {
            var folder = Root + "/RoundTrip";
            var b = Node(folder + "/B/Dependency.asset");
            Node(folder + "/A/Owner.asset", b);
            var collector = Collect(folder);
            var bundles = Bundles(collector);
            Check(bundles.SelectMany(bi => bi.AssetList).All(a => a == a.ToLowerInvariant() && a.StartsWith("assets/")), "Every collected resource uses a full lowercase project path.");
            var expected = bundles.ToDictionary(bi => bi.BundlePath, bi => (Assets: bi.AssetList.OrderBy(a => a).ToArray(), Dependencies: bi.DependencyList.OrderBy(a => a).ToArray()));
            var pack = new VersionPackInfo("review", "windows", false, false, bundles);
            var restored = VersionPackInfo.FromBinaryData(pack.ToBinaryData());
            restored.Initialize();
            foreach (var bundle in restored.BundleList)
            {
                Check(expected[bundle.BundlePath].Assets.SequenceEqual(bundle.AssetList.OrderBy(a => a)), "Existing codec restores resources for " + bundle.BundlePath);
                Check(expected[bundle.BundlePath].Dependencies.SequenceEqual(bundle.DependencyList.OrderBy(a => a)), "Existing codec restores dependencies for " + bundle.BundlePath);
            }
        }
        private static void DottedDirectory()
        {
            var folder = Root + "/Dotted";
            var dependency = folder + "/Textures.v2/Shared.asset";
            var shared = Node(dependency);
            var owner = folder + "/Owners/Owner.asset";
            Node(owner, shared);
            var collector = Collect(folder);
            var depBundle = Owner(collector, dependency);
            Check(depBundle.BundlePath == "__collectorreview20261022/dotted/textures.v2.ab", "Dotted source folder retains its own Bundle.");
            Check(Owner(collector, owner).DependencyList.Single() == depBundle.BundlePath, "Dependency edge uses the same corrected directory-based grouping.");
            Check(depBundle.AssetList.Single() == dependency.ToLowerInvariant() && collector.GetAssetSourcePath(dependency.ToLowerInvariant()) == dependency, "Full identity and source casing are preserved.");
            var pack = VersionPackInfo.FromBinaryData(new VersionPackInfo("dots", "windows", false, false, Bundles(collector)).ToBinaryData());
            pack.Initialize();
            Check(pack.GetBundleInfoFromAssetPath(dependency).BundlePath == depBundle.BundlePath, "Corrected dotted Bundle remains indexed after manifest round trip.");
        }

        private static void DottedIgnored()
        {
            var folder = Root + "/Merge.v2";
            var ignored = folder + "/Child.v3";
            var path = ignored + "/Item.asset";
            Node(path);
            var collector = Collect(folder, Setting(ignored));
            Check(Owner(collector, path).BundlePath == "__collectorreview20261022/merge.v2.ab", "IgnorePathList merges the dotted child into the dotted parent exactly once.");
            const string rootFolder = "Assets/StandaloneAssets/__CollectorRoot.v2";
            var rootPath = rootFolder + "/Item.asset";
            Node(rootPath);
            collector = Collect(rootFolder);
            Check(Owner(collector, rootPath).BundlePath == "__collectorroot.v2.ab", "A dotted top-level folder is not assigned to default.ab.");
        }

        private static void CrossCycle()
        {
            var folder = Root + "/CrossCycle";
            var a = Node(folder + "/A/Node.asset"); var b = Node(folder + "/B/Node.asset", a);
            a.References = new UnityEngine.Object[] { b }; EditorUtility.SetDirty(a); AssetDatabase.SaveAssets();
            var collector = Collect(folder);
            var pack = new VersionPackInfo("cycle", "windows", false, false, Bundles(collector));
            var actualCycles = AssetBundleDependencyValidator.FindUniqueCycles(pack);
            Check(actualCycles.Count > 0, "The produced Bundle graph contains the expected cross-Bundle cycle.");
            try
            {
                var publicCollector = new ResCollector(Array.Empty<string>());
                var rejected = false;
                try { publicCollector.GetVersionPackInfo("cycle", ValidationEnvironment.BundleTarget, false, false); }
                catch (InvalidOperationException ex) when (ex.Message.Contains("发现循环依赖")) { rejected = true; }
                var cached = Field<VersionPackInfo>(publicCollector, "_packInfo");
                Active.evidence.Add("Public collector rejected cycle=" + rejected + "; cached invalid pack=" + (cached != null));
                if (cached != null) Active.evidence.Add("Cycles in cached pack=" + AssetBundleDependencyValidator.FindUniqueCycles(cached).Count);
                Check(rejected && cached == null, "Public GetVersionPackInfo must reject cyclic graphs before caching.");
                rejected = false;
                try { publicCollector.GetVersionPackInfo("cycle", ValidationEnvironment.BundleTarget, false, false); }
                catch (InvalidOperationException ex) when (ex.Message.Contains("发现循环依赖")) { rejected = true; }
                Check(rejected, "Retry also validates and rejects the cycle.");
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
            Check(!AssetDatabase.IsValidFolder(folder), "The intentional cycle fixture is removed before other suites collect the project.");
        }
    }
}
