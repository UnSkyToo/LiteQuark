using System;

namespace LiteQuark.Runtime
{
    public static class AssetPath
    {
        private const string RootPrefix = LiteConst.AssetRootPath + "/";

        internal static string NormalizeBundlePath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : PathUtils.UnifyPath(path).Trim('/').ToLowerInvariant();
        }

        internal static string Resolve(string input) => ToAssetKey(ToProjectPath(input));

        internal static string ResolveProjectAssetPath(string input) => ToAssetKey(ProjectPath(input, allowRelative: false));

        internal static bool TryResolve(string input, out string assetKey)
        {
            try
            {
                assetKey = Resolve(input);
                return true;
            }
            catch (ArgumentException)
            {
                assetKey = null;
                return false;
            }
        }

        internal static string ResolveScene(string input)
        {
            var sceneKey = Resolve(input);
            if (!sceneKey.EndsWith(".unity", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Scene path must end with .unity: {input}", nameof(input));
            }
            return sceneKey;
        }

        private static string ToAssetKey(string path)
        {
            var dot = path.LastIndexOf('.');
            if (dot <= path.LastIndexOf('/') + 1 || dot == path.Length - 1)
            {
                throw new ArgumentException($"Asset path must include a file extension: {path}", nameof(path));
            }
            return path.ToLowerInvariant();
        }

        public static string ToProjectPath(string input) => ProjectPath(input, allowRelative: true);

        public static string ToRootRelativePath(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                return string.Empty;
            }

            var path = ProjectPath(projectPath, allowRelative: false);
            var relative = StripAssetRoot(path);
            return IsProjectPath(relative) ? path : relative;
        }

        internal static string StripAssetRoot(string path)
        {
            if (path.Equals(LiteConst.AssetRootPath, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }
            return path.StartsWith(RootPrefix, StringComparison.OrdinalIgnoreCase) ? path.Substring(RootPrefix.Length) : path;
        }

        private static bool IsProjectPath(string path)
        {
            return path.StartsWith(LiteConst.AssetRootName + "/", StringComparison.OrdinalIgnoreCase) ||
                path.Equals(LiteConst.AssetRootName, StringComparison.OrdinalIgnoreCase);
        }

        private static string ProjectPath(string input, bool allowRelative)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Asset path is empty.", nameof(input));
            }

            var path = PathUtils.UnifyPath(input);
            if (!IsProjectPath(path))
            {
                if (!allowRelative)
                {
                    throw new ArgumentException($"Expected an Assets project path: {input}", nameof(input));
                }
                path = RootPrefix + path;
            }

            if (path.Contains("/../") || path.EndsWith("/..", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Parent traversal is not supported: {input}", nameof(input));
            }
            return path;
        }
    }
}
