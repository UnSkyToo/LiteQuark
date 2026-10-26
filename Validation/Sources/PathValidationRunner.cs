using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal static class PathValidationRunner
    {
        private static readonly Report Results = new();
        private static Case Active;
        private static string Scratch;
        private static string Root => Directory.GetParent(Application.dataPath).Parent.FullName;
        [Serializable] private sealed class Report { public string unityVersion; public string fatalError; public List<Case> cases = new(); }
        [Serializable] private sealed class Case { public string name; public bool passed; public int checks; public string failure; public List<string> evidence = new(); }
        public static void Begin()
        {
            var reports = ValidationEnvironment.Reports("paths");
            Directory.CreateDirectory(reports);
            Scratch = Path.Combine(Root, "Scratch", "PathUtils-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Scratch);
            try
            {
                Run("01 Separator normalization preserves roots and casing", Normalize);
                Run("02 Parent paths preserve native filesystem roots", Parents);
                Run("03 Relative paths use filesystem semantics", Relative);
                Run("04 Asset root wrapper preserves outside paths and empty fields", AssetRelative);
                Run("05 Copy filters preserve names and dotted directories", CopyTree);
                Run("06 Empty and fully filtered trees create no directories", EmptyTrees);
                Run("07 Copy preserves existing destination files and empty directories", ExistingDestination);
                Run("08 Copy rejects self and descendants before writing", RejectTargets);
                Run("09 Rename creates dotted parents and supports bare filenames", Rename);
                Run("10 Five obsolete APIs are absent", RemovedApis);
                Run("11 Navigation saves to a dotted parent directory", Navigation);
                Run("12 Editor workspace path uses the actual project parent", WorkspacePath);
                Run("13 ConcatPath retains standard filesystem composition", FileComposition);
                Run("14 URI directory composition preserves escaping and suffixes", UriComposition);
                Run("15 File and jar URI forms preserve their schemes", LocalUriForms);
                Run("16 StreamingAssets preserves the actual Editor file location", StreamingLocation);
            }
            catch (Exception ex) { Results.fatalError = ex.ToString(); }
            finally
            {
                var prefix = Path.GetFullPath(Path.Combine(Root, "Scratch")).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (Path.GetFullPath(Scratch).StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && Path.GetFileName(Scratch).StartsWith("PathUtils-", StringComparison.Ordinal))
                    Directory.Delete(Scratch, true);
                Results.unityVersion = Application.unityVersion;
                File.WriteAllText(Path.Combine(reports, "results.json"), JsonUtility.ToJson(Results, true));
                EditorApplication.Exit(Results.fatalError == null && Results.cases.All(c => c.passed) ? 0 : 2);
            }
        }
        private static void Run(string name, Action test)
        {
            Active = new Case { name = name }; Results.cases.Add(Active);
            try { test(); Active.passed = true; } catch (Exception ex) { Active.failure = ex.ToString(); }
            Debug.Log("PATH_UTILS_REVIEW " + (Active.passed ? "PASS " : "FAIL ") + name);
        }
        private static void Check(bool value, string evidence)
        {
            Active.checks++;
            if (!value) throw new Exception(evidence);
            Active.evidence.Add(evidence);
        }
        private static string At(string relative) => Path.Combine(Scratch, relative);

        private static void FileComposition()
        {
            var windows = Application.platform == RuntimePlatform.WindowsEditor;
            foreach (var item in new[] {
                (Root: "C:/Base", Child: "Sub/File.txt", Expected: "C:/Base/Sub/File.txt"),
                (Root: "C:/Base", Child: "/Other/File.txt", Expected: "/Other/File.txt"),
                (Root: "C:/Base", Child: "\\Other\\File.txt", Expected: windows ? "/Other/File.txt" : "C:/Base//Other/File.txt"),
                (Root: "C:/Base", Child: "D:/Other/File.txt", Expected: windows ? "D:/Other/File.txt" : "C:/Base/D:/Other/File.txt"),
                (Root: "C:/", Child: "", Expected: "C:/"),
                (Root: "Assets/StandaloneAssets", Child: "UI/My Icon.png", Expected: "Assets/StandaloneAssets/UI/My Icon.png"),
                (Root: "\\\\server\\share", Child: "Dir\\File.txt", Expected: "//server/share/Dir/File.txt"),
                (Root: "C:/Base", Child: "../File.txt", Expected: "C:/Base/../File.txt"),
                (Root: "", Child: "File.txt", Expected: "File.txt"),
                (Root: "/tmp/Base", Child: "Sub/File.txt", Expected: "/tmp/Base/Sub/File.txt"),
                (Root: "/tmp/Base", Child: "/Other/File.txt", Expected: "/Other/File.txt") })
                Check(PathUtils.ConcatPath(item.Root, item.Child) == item.Expected, "Exact file composition: " + item.Root + " + " + item.Child + " -> " + item.Expected);
            Check(PathUtils.ConcatPath("C:/Base", "D:/Other", "File.txt") == (windows ? "D:/Other/File.txt" : "C:/Base/D:/Other/File.txt"), "A drive path is rooted only on Windows (middle argument).");
            Check(PathUtils.ConcatPath("C:/Base", "Sub", "D:/File.txt") == (windows ? "D:/File.txt" : "C:/Base/Sub/D:/File.txt"), "A drive path is rooted only on Windows (final argument).");
            Check(PathUtils.ConcatPath("Assets", "StandaloneAssets", "UI/Icon.png") == "Assets/StandaloneAssets/UI/Icon.png", "Three-part project path composition preserves relative directories.");
        }

        private static void UriComposition()
        {
            foreach (var item in new[] {
                (Root: "https://cdn.example/res", Child: "UI/Icon.ab", Expected: "https://cdn.example/res/UI/Icon.ab"),
                (Root: "https://cdn.example/res/", Child: "/UI/Icon.ab/", Expected: "https://cdn.example/res/UI/Icon.ab"),
                (Root: "https://cdn.example/base.json", Child: "Icon.ab", Expected: "https://cdn.example/base.json/Icon.ab"),
                (Root: "https://cdn.example/res?token=a%2Bb#part", Child: "UI/My Icon.ab", Expected: "https://cdn.example/res/UI/My%20Icon.ab?token=a%2Bb#part"),
                (Root: "https://cdn.example/base%20dir", Child: "UI/中文 #100%.ab", Expected: "https://cdn.example/base%20dir/UI/%E4%B8%AD%E6%96%87%20%23100%25.ab"),
                (Root: "https://cdn.example/res", Child: "a%20b.ab", Expected: "https://cdn.example/res/a%2520b.ab"),
                (Root: "https://cdn.example/res", Child: "UI\\Icon.ab", Expected: "https://cdn.example/res/UI/Icon.ab"),
                (Root: "https://cdn.example/res?token=x#part", Child: "", Expected: "https://cdn.example/res?token=x#part"),
                (Root: "https://cdn.example/res", Child: "a//b.ab", Expected: "https://cdn.example/res/a//b.ab") })
            {
                var actual = PathUtils.ConcatUri(item.Root, item.Child);
                Check(actual == item.Expected, "URI expected=[" + item.Expected + "]; actual=[" + actual + "]");
            }
            Check(PathUtils.ConcatUri("https://cdn.example/base%20dir?token=a%2Bb#part", "windows/0.4", "UI/My Icon.ab") ==
                "https://cdn.example/base%20dir/windows/0.4/UI/My%20Icon.ab?token=a%2Bb#part", "Repeated composition retains existing percent encoding, query and fragment.");
            Check(PathUtils.ConcatUri("http://localhost:8000/res", "data.ab") == "http://localhost:8000/res/data.ab", "HTTP port remains intact; no request is sent.");
        }

        private static void LocalUriForms()
        {
            Check(PathUtils.ConcatUri("file:///C:/My%20Game/StreamingAssets", "LiteQuark", "UI/My Icon.ab") ==
                "file:///C:/My%20Game/StreamingAssets/LiteQuark/UI/My%20Icon.ab", "File URI keeps its scheme and existing escaped base path.");
            Check(PathUtils.ConcatUri("jar:file:///data/app/base.apk!/assets", "LiteQuark", "UI/My Icon.ab") ==
                "jar:file:///data/app/base.apk!/assets/LiteQuark/UI/My%20Icon.ab", "APK-style URI preserves jar:file and !/assets components.");
            Check(PathUtils.ConcatUri("jar:file:///data/app/base.apk!/assets", "") == "jar:file:///data/app/base.apk!/assets", "An empty appended path preserves the jar URI.");
        }

        private static void StreamingLocation()
        {
            var root = Application.streamingAssetsPath;
            Check(root.IndexOf("://", StringComparison.Ordinal) < 0, "This desktop Editor run exercises the actual filesystem StreamingAssets branch.");
            Check(PathUtils.GetStreamingAssetsPath("LiteQuark") == PathUtils.UnifyPath(Path.Combine(root, "LiteQuark")), "One-part StreamingAssets path retains the existing Editor directory.");
            Check(PathUtils.GetStreamingAssetsPath("LiteQuark", "UI/My Icon#100%.ab") ==
                PathUtils.UnifyPath(Path.Combine(root, "LiteQuark", "UI/My Icon#100%.ab")), "Local StreamingAssets filenames remain unescaped filesystem names.");
            Check(PathUtils.GetStreamingAssetsPath("") == PathUtils.UnifyPath(root), "Empty child returns the actual StreamingAssets root.");
        }
        private static void Write(string relative, string content = "probe")
        {
            var path = At(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }
        private static void Normalize()
        {
            foreach (var pair in new[] { ("C:\\", "C:/"), ("/", "/"), ("Assets\\Data.v2\\", "Assets/Data.v2/"), ("\\\\server\\share\\", "//server/share/"), ("a//b/", "a//b/"), ("", "") })
                Check(PathUtils.UnifyPath(pair.Item1) == pair.Item2, "Unify only replaces separators: " + pair.Item1);
            Check(AssetPath.NormalizeBundlePath("/Assets\\Data.v2/") == "assets/data.v2", "Bundle formatting trims boundary separators and lowercases.");
        }
        private static void Parents()
        {
            var windows = Application.platform == RuntimePlatform.WindowsEditor;
            foreach (var pair in new[] {
                ("C:/foo", windows ? "C:/" : "C:"), ("C:/", ""), ("C:\\", ""),
                ("/", ""), ("\\", ""),
                ("Assets/Data.v2/", "Assets"), ("Assets/Data.v2///", "Assets"),
                ("Assets\\Data.v2\\item", "Assets/Data.v2"),
                ("file.txt", ""), ("", ""),
                (windows ? "//server/share/" : "/Users/test/", windows ? "" : "/Users"),
                (windows ? "\\\\server\\share\\" : "/Users/test/data/", windows ? "" : "/Users/test") })
            {
                var actual = PathUtils.GetParentPath(pair.Item1);
                Check(actual == pair.Item2, "Production parent input=" + pair.Item1 + "; actual=[" + actual + "]; expected=[" + pair.Item2 + "]");
            }
            var children = windows
                ? new[] { "//server/share/folder", "//server/share/folder/", "\\\\server\\share\\folder\\" }
                : new[] { "/Users/test/folder", "/Users/test/folder/", "\\Users\\test\\folder\\" };
            foreach (var path in children)
            {
                var actual = PathUtils.GetParentPath(path);
                Check(actual.TrimEnd('/') == (windows ? "//server/share" : "/Users/test"), "Native root child parent: " + path + " -> " + actual);
            }
        }
        private static void Relative()
        {
            var basePath = At("base");
            Check(PathUtils.GetRelativePath(basePath, Path.Combine(basePath, "nested/base/a.txt")) == "nested/base/a.txt", "Repeated middle directory remains intact.");
            Check(PathUtils.GetRelativePath(basePath, At("baseBackup/a.txt")) == "../baseBackup/a.txt", "Similar directory prefix is not treated as a child.");
            Check(PathUtils.GetRelativePath(basePath, basePath) == ".", "Equal paths return dot.");
            Check(PathUtils.GetRelativePath(basePath.Replace('/', '\\'), (basePath + "/a.txt").Replace('/', '\\')) == "a.txt", "Backslash paths are normalized before comparison.");
            Check(PathUtils.GetRelativePath(basePath, basePath + "/sub/../a.txt") == "a.txt", "Filesystem relative calculation resolves parent segments.");
        }
        private static void AssetRelative()
        {
            foreach (var pair in new[] {
                ("Assets/StandaloneAssets/Data/A.asset", "Data/A.asset"),
                ("Assets/StandaloneAssets/Data/Assets/StandaloneAssets/A.asset", "Data/Assets/StandaloneAssets/A.asset"),
                ("Assets/StandaloneAssetsBackup/A.asset", "Assets/StandaloneAssetsBackup/A.asset"),
                ("Assets/Shared/A.asset", "Assets/Shared/A.asset"),
                ("assets\\standaloneassets\\Data\\A.asset", "Data/A.asset"),
                ("Assets/StandaloneAssets/", ""), ("", "") })
                Check(AssetPath.ToRootRelativePath(pair.Item1) == pair.Item2, "Asset wrapper preserves intended representation: " + pair.Item1);
            Check(AssetPath.ToProjectPath("Data/A.asset") == "Assets/StandaloneAssets/Data/A.asset", "Business-relative input is based on Assets/StandaloneAssets and preserves casing.");
            Check(AssetPath.Resolve("Data/A.asset") == "assets/standaloneassets/data/a.asset", "Public resource keys use the same business root.");
            foreach (var pair in new[] {
                ("assets.json", "Assets/StandaloneAssets/assets.json"),
                ("assetsbackup/Icon.png", "Assets/StandaloneAssets/assetsbackup/Icon.png"),
                ("packages/demo/Icon.png", "Assets/StandaloneAssets/packages/demo/Icon.png"),
                ("Packages/demo/Icon.png", "Assets/StandaloneAssets/Packages/demo/Icon.png"),
                ("Assets/Shared/Icon.png", "Assets/Shared/Icon.png"),
                ("Assets", "Assets"), ("UI/", "Assets/StandaloneAssets/UI"),
                ("Prefabs\\./Player.prefab", "Assets/StandaloneAssets/Prefabs/Player.prefab") })
                Check(AssetPath.ToProjectPath(pair.Item1) == pair.Item2, "ToProjectPath preserves source spelling and interprets input boundaries: " + pair.Item1);
            foreach (var full in new[] { "Assets/StandaloneAssets/Prefabs/Player.prefab", "Assets/Shared/Other.prefab" })
                Check(AssetPath.ToProjectPath(AssetPath.ToRootRelativePath(full)) == full, "Authoring conversion round trip: " + full);
            Check(AssetPath.ToRootRelativePath((string)null) == string.Empty, "Null editor field becomes an empty authoring value.");
            Check(AssetPath.ToRootRelativePath(new[] { "Assets/StandaloneAssets/A.asset", "Assets/Shared/B.asset", "", null })
                .SequenceEqual(new[] { "A.asset", "Assets/Shared/B.asset", "", "" }), "Array conversion preserves root-external paths and empty fields.");
            Check(AssetPath.ToRootRelativePath(Array.Empty<string>()).Length == 0, "Empty array is valid.");
            var nullArrayRejected = false;
            try { AssetPath.ToRootRelativePath((string[])null); } catch (ArgumentNullException) { nullArrayRejected = true; }
            Check(nullArrayRejected, "Null array is rejected.");
            foreach (var invalid in new[] { "../Icon.png", "C:/Icon.png", "/Assets/Icon.png", "https://host/Icon.png" })
            {
                var rejected = false;
                try { AssetPath.ToProjectPath(invalid); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "Project path conversion rejects invalid input: " + invalid);
            }
            Check(AssetPath.ToProjectPath("assets/demo/Icon.png") == "assets/demo/Icon.png", "Public assets-prefix ambiguity remains deferred, with no new fallback.");

        }
        private static void CopyTree()
        {
            Write("tree/src/data.txt", "root"); Write("tree/src/data.v2/LICENSE", "extensionless");
            Write("tree/src/data.v2/skip.manifest");
            Directory.CreateDirectory(At("tree/src/empty"));
            PathUtils.CopyDirectory(At("tree/src"), At("tree/dst"), p => !p.EndsWith(".manifest", StringComparison.Ordinal));
            Check(File.ReadAllText(At("tree/dst/data.txt")) == "root", "Root file copied without filename rewriting.");
            Check(File.ReadAllText(At("tree/dst/data.v2/LICENSE")) == "extensionless", "Dotted directory and extensionless file copied.");
            Check(!File.Exists(At("tree/dst/data.v2/skip.manifest")), "File filter applied recursively.");
            Check(!Directory.Exists(At("tree/dst/empty")), "Empty source branch omitted.");
            Check(Directory.GetDirectories(At("tree/dst"), "*", SearchOption.AllDirectories).All(d => Directory.EnumerateFileSystemEntries(d).Any()), "Copy generated no empty target directories.");
        }
        private static void EmptyTrees()
        {
            Directory.CreateDirectory(At("empty/src/nested/empty"));
            PathUtils.CopyDirectory(At("empty/src"), At("empty/dst"));
            Check(!Directory.Exists(At("empty/dst")), "An entirely empty source tree creates no destination.");
            Write("filtered/src/one.manifest"); Write("filtered/src/child/two.manifest");
            PathUtils.CopyDirectory(At("filtered/src"), At("filtered/dst"), _ => false);
            Check(!Directory.Exists(At("filtered/dst")), "An entirely filtered source tree creates no destination.");
            PathUtils.CopyDirectory(At("missing/src"), At("missing/dst"));
            Check(!Directory.Exists(At("missing/dst")), "A missing source creates no destination.");
        }
        private static void ExistingDestination()
        {
            Write("existing/src/skip.manifest"); Write("existing/src/child/skip.manifest");
            Write("existing/dst/keep.txt", "keep-root"); Write("existing/dst/child/keep.txt", "keep-child");
            Directory.CreateDirectory(At("existing/dst/old-empty"));
            PathUtils.CopyDirectory(At("existing/src"), At("existing/dst"), _ => false);
            Check(File.ReadAllText(At("existing/dst/keep.txt")) == "keep-root", "Fully filtered copy preserves preexisting root file.");
            Check(File.ReadAllText(At("existing/dst/child/keep.txt")) == "keep-child", "Fully filtered nested copy preserves preexisting child file.");
            Check(Directory.Exists(At("existing/dst/old-empty")), "Preexisting empty target directories are not deleted.");
            Write("existing/src/keep.txt", "replacement");
            PathUtils.CopyDirectory(At("existing/src"), At("existing/dst"), p => p.EndsWith(".txt", StringComparison.Ordinal));
            Check(File.ReadAllText(At("existing/dst/keep.txt")) == "replacement" && File.ReadAllText(At("existing/dst/child/keep.txt")) == "keep-child", "Accepted same-name file is overwritten; unrelated target content survives.");
        }
        private static void RejectTargets()
        {
            Write("guard/src/a.txt");
            foreach (var dest in new[] { At("guard/src"), At("guard/src/child"), At("guard/src/sub/../child") })
            {
                var rejected = false;
                try { PathUtils.CopyDirectory(At("guard/src"), dest); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "Self or descendant destination rejected: " + dest);
            }
            Check(!Directory.Exists(At("guard/src/child")), "Rejection happens before destination creation.");
            PathUtils.CopyDirectory(At("guard/src"), At("guard/srcBackup"));
            Check(File.Exists(At("guard/srcBackup/a.txt")), "Similar sibling name is a valid destination.");
        }
        private static void Rename()
        {
            Write("rename/source.txt", "renamed");
            Check(PathUtils.RenameFile(At("rename/source.txt"), At("rename/dest.v2/item.txt")), "Rename succeeds with a new dotted parent.");
            Check(File.ReadAllText(At("rename/dest.v2/item.txt")) == "renamed" && !File.Exists(At("rename/source.txt")), "Rename preserves data and removes old file.");
            Check(!PathUtils.RenameFile(At("rename/missing.txt"), At("rename/no-parent/item.txt")) && !Directory.Exists(At("rename/no-parent")), "Missing source is a no-op.");
            var cwd = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = At("rename");
                File.WriteAllText("bare-source.txt", "bare");
                Check(PathUtils.RenameFile("bare-source.txt", "bare-target.txt") && File.ReadAllText("bare-target.txt") == "bare", "Bare filename destination does not attempt creating an empty parent.");
            }
            finally { Environment.CurrentDirectory = cwd; }
        }
        private static void RemovedApis()
        {
            foreach (var name in new[] { "PathIsFile", "GetPathFromFullPath", "FileExists", "DirectoryExists", "CreateDirectory" })
                Check(typeof(PathUtils).GetMethod(name) == null, "Removed obsolete public API: " + name);
        }
        private static void Navigation()
        {
            var data = new NavigationData(); data.AddPath("Assets/Example.asset");
            var path = At("navigation.v2/config.json");
            NavigationData.ToJson(data, path);
            Check(File.Exists(path), "Updated caller creates the JSON file parent, including dots.");
            Check(NavigationData.FromJson(path).GetPathList().SequenceEqual(data.GetPathList()), "Navigation data survives the updated directory creation flow.");
        }

        private static void WorkspacePath()
        {
            var expected = PathUtils.UnifyPath(Path.Combine(Directory.GetParent(Application.dataPath).FullName, LiteConst.Tag, "Build"));
            Check(LiteEditorUtils.GetLiteQuarkRootPath("Build") == expected, "Editor workspace derives from the actual parent of Application.dataPath.");
            Check(PathUtils.GetParentPath("C:/AssetsWork/Game/Assets") == "C:/AssetsWork/Game", "Parent calculation preserves Assets in an ancestor name.");
            foreach (var relative in new[] { "", "Navigation/PathList.json", "Build/StandaloneWindows64/1.2.3/Release" })
            {
                var workspace = Path.Combine(Directory.GetParent(Application.dataPath).FullName, LiteConst.Tag);
                var expectedPath = PathUtils.UnifyPath(Path.Combine(workspace, relative));
                Check(LiteEditorUtils.GetLiteQuarkRootPath(relative) == expectedPath, "Editor caller keeps the existing workspace layout: " + relative);
            }
            foreach (var obsolete in new[] { "GetFullPathInRuntime", "GetLiteQuarkRootPath", "ToFlatPath" })
                Check(typeof(PathUtils).GetMethod(obsolete) == null, "Business-specific helper removed from PathUtils: " + obsolete);

        }
    }
}
