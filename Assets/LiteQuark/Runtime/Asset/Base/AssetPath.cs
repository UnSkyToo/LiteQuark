using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public static class AssetPath
    {
        private static readonly char[] InvalidChars = { ':', '*', '?', '"', '<', '>', '|' };

        // 规范化逻辑 Bundle 路径，不补资源根目录。
        public static string NormalizeBundlePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return PathUtils.UnifyPath(path).Trim('/').ToLowerInvariant();
        }

        // 仅接受完整 Assets/... 资源路径，返回规范 key。
        // 不将内部路径作为业务相对路径补全。
        public static string ResolveProjectAssetPath(string input)
        {
            NormalizeProjectPath(input);
            return Resolve(input);
        }

        public static bool TryResolve(string input, out string assetKey)
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

        // 接受业务相对路径或 Assets/...，返回资源的规范 key。
        public static string Resolve(string input)
        {
            var path = ToProjectPath(input);

            var source = input.Replace('\\', '/');
            if (source.EndsWith("/", StringComparison.Ordinal) ||
                source.EndsWith("/.", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Expected an asset file path: {input}", nameof(input));
            }

            var slashIndex = path.LastIndexOf('/');
            var dotIndex = path.LastIndexOf('.');

            if (dotIndex <= slashIndex + 1 || dotIndex == path.Length - 1)
            {
                throw new ArgumentException($"Asset path must include a file extension: {input}", nameof(input));
            }

            return path.ToLowerInvariant();
        }

        public static string ResolveScene(string input)
        {
            var sceneKey = Resolve(input);

            if (!sceneKey.EndsWith(".unity", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Scene path must end with .unity: {input}", nameof(input));
            }

            return sceneKey;
        }

        public static string ToRootRelativePath(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                return string.Empty;
            }

            var path = NormalizeProjectPath(projectPath);
            var root = NormalizeProjectPath(LiteConst.AssetRootPath);

            if (string.Equals(path, root, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return TryGetRelativeToRoot(path, root, out var relativePath) ? relativePath : path;
        }

        public static string[] ToRootRelativePath(string[] projectPaths)
        {
            if (projectPaths == null)
            {
                throw new ArgumentNullException(nameof(projectPaths));
            }

            var result = new string[projectPaths.Length];

            for (var index = 0; index < projectPaths.Length; index++)
            {
                result[index] = ToRootRelativePath(projectPaths[index]);
            }

            return result;
        }

        // 仅处理严格位于 rootPath 下的路径，不把根目录自身当作子路径。
        // 返回值保留 projectPath 的大小写。
        public static bool TryGetRelativeToRoot(string projectPath, string rootPath, out string relativePath)
        {
            var path = NormalizeProjectPath(projectPath);
            var root = NormalizeProjectPath(rootPath);
            var prefix = root + "/";

            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = null;
                return false;
            }

            relativePath = path.Substring(prefix.Length);
            return true;
        }

        // 用于构建、编辑器和目录操作；保留大小写，允许目录路径。
        public static string NormalizeProjectPath(string input)
        {
            var path = Normalize(input);

            if (!path.Equals("Assets", StringComparison.OrdinalIgnoreCase) && !IsProjectPath(path))
            {
                throw new ArgumentException($"Expected an Assets project path: {input}", nameof(input));
            }

            return path;
        }

        // 接受业务相对路径或完整 Assets 项目路径。
        // 保留输入大小写，允许目录；返回项目路径，不是磁盘绝对路径。
        public static string ToProjectPath(string input)
        {
            var path = Normalize(input);

            if (path.Equals(LiteConst.AssetRootName, StringComparison.OrdinalIgnoreCase) || IsProjectPath(path))
            {
                return path;
            }

            return $"{NormalizeProjectPath(LiteConst.AssetRootPath)}/{path}";
        }

        private static bool IsProjectPath(string path)
        {
            return path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Asset path is empty.", nameof(input));
            }

            var path = input.Replace('\\', '/');

            if (path.StartsWith("/", StringComparison.Ordinal) || path.IndexOfAny(InvalidChars) >= 0)
            {
                throw new ArgumentException($"Invalid asset path: {input}", nameof(input));
            }

            foreach (var ch in path)
            {
                if (char.IsControl(ch))
                {
                    throw new ArgumentException($"Asset path contains a control character: {input}", nameof(input));
                }
            }

            var segments = new List<string>();

            foreach (var segment in path.Split('/'))
            {
                if (segment.Length == 0 || segment == ".")
                {
                    continue;
                }

                if (segment == "..")
                {
                    throw new ArgumentException($"Parent traversal is not supported: {input}", nameof(input));
                }

                segments.Add(segment);
            }

            if (segments.Count == 0)
            {
                throw new ArgumentException("Asset path is empty.", nameof(input));
            }

            return string.Join("/", segments);
        }
    }
}
