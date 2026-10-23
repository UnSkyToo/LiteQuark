using System;
using System.Collections.Generic;

namespace LiteQuark.Runtime
{
    public static class AssetPath
    {
        private static readonly char[] InvalidChars = { ':', '*', '?', '"', '<', '>', '|' };

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
            var path = Normalize(input);

            var source = input.Replace('\\', '/');
            if (source.EndsWith("/", StringComparison.Ordinal) || source.EndsWith("/.", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Expected an asset file path: {input}", nameof(input));
            }

            if (IsProjectPath(path))
            {
            }
            else if (path.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Unsupported asset path: {input}", nameof(input));
            }
            else
            {
                path = $"{NormalizeProjectPath(LiteConst.AssetRootPath)}/{path}";
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
