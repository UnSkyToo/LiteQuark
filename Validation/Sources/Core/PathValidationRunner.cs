using System;
using System.IO;
using System.Linq;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed class PathValidationRunner : EditorValidation
    {
        private static string Scratch;
        private static string Root => Directory.GetParent(Application.dataPath).Parent.FullName;
        internal static void RunCases()
        {
            Scratch = Path.Combine(Root, "Scratch", "PathUtils-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Scratch);
            try
            {
                Run("paths/host-path", () =>
                {
                    Normalize();
                    Parents();
                    Relative();
                    AssetRelative();
                    WorkspacePath();
                    FileComposition();
                });
                Run("paths/uri", () =>
                {
                    UriComposition();
                    LocalUriForms();
                    StreamingLocation();
                });
                Run("paths/copy-tree", CopyTree);
                Run("paths/rename", Rename);
            }
            finally
            {
                var prefix = Path.GetFullPath(Path.Combine(Root, "Scratch")).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (Path.GetFullPath(Scratch).StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && Path.GetFileName(Scratch).StartsWith("PathUtils-", StringComparison.Ordinal))
                    Directory.Delete(Scratch, true);
            }
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
                ("Assets/StandaloneAssets/aSseTs/UI/Hud.prefab", "Assets/StandaloneAssets/aSseTs/UI/Hud.prefab"),
                ("Assets/StandaloneAssets/AssetsBackup/UI/Hud.prefab", "AssetsBackup/UI/Hud.prefab"),
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
                ("Assets", "Assets"), ("UI", "Assets/StandaloneAssets/UI"),
                ("Prefabs\\Player.prefab", "Assets/StandaloneAssets/Prefabs/Player.prefab") })
                Check(AssetPath.ToProjectPath(pair.Item1) == pair.Item2, "ToProjectPath preserves source spelling and interprets input boundaries: " + pair.Item1);
            foreach (var full in new[] { "Assets/StandaloneAssets/Prefabs/Player.prefab", "Assets/Shared/Other.prefab",
                "Assets/StandaloneAssets/aSseTs/UI/Hud.prefab", "Assets/UI/Hud.prefab" })
                Check(AssetPath.ToProjectPath(AssetPath.ToRootRelativePath(full)) == full, "Authoring conversion round trip: " + full);
            Check(AssetPath.ToRootRelativePath(null) == string.Empty, "Null editor field becomes an empty authoring value.");
        }

        private static void CopyTree()
        {
            var source = At("tree/src");
            var target = At("tree/dst");
            Directory.CreateDirectory(At("tree/src/empty"));
            PathUtils.CopyDirectory(source, target);
            Check(!Directory.Exists(target), "An empty source creates no destination.");
            Write("tree/src/data.v2/skip.manifest");
            bool Include(string path) => !path.EndsWith(".manifest", StringComparison.Ordinal);
            PathUtils.CopyDirectory(source, target, Include);
            Check(!Directory.Exists(target), "A fully filtered tree creates no destination.");

            Write("tree/src/data.v2/LICENSE", "new");
            Write("tree/dst/data.v2/LICENSE", "old");
            Write("tree/dst/unrelated.txt", "keep");
            Directory.CreateDirectory(At("tree/dst/old-empty"));
            foreach (var path in new[] { source, At("tree/src/sub/../child") })
            {
                var rejected = false;
                try { PathUtils.CopyDirectory(source, path); } catch (ArgumentException) { rejected = true; }
                Check(rejected && !Directory.Exists(At("tree/src/child")), "Reject self or descendant before writing.");
            }
            PathUtils.CopyDirectory(source, target, Include);
            Check(File.ReadAllText(At("tree/dst/data.v2/LICENSE")) == "new" && File.ReadAllText(At("tree/dst/unrelated.txt")) == "keep",
                "Copy replaces only included files and preserves actual names.");
            Check(!File.Exists(At("tree/dst/data.v2/skip.manifest")) && !Directory.Exists(At("tree/dst/empty")) &&
                Directory.Exists(At("tree/dst/old-empty")), "Filtering and empty branches preserve unrelated destination content.");
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
        }
    }
}
