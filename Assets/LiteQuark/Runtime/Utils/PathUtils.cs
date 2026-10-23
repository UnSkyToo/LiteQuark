using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class PathUtils
    {
        public const char DirectorySeparatorChar = '/';

        public static string UnifyPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return path.Replace('\\', DirectorySeparatorChar);
        }

        public static string FormatAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return UnifyPath(path).Trim(DirectorySeparatorChar).ToLowerInvariant();
        }

        private static readonly string InvalidFlatChars = "/ \\:*?\"\'<>|~#%&@+{}[]";
        public static string ToFlatPath(string path)
        {
            return InvalidFlatChars.Aggregate(path, static (current, ch) => current.Replace(ch, '_'));
        }

        public static string ConcatPath(string path1, string path2)
        {
            var path = Path.Combine(path1, path2.TrimStart(DirectorySeparatorChar));
            return UnifyPath(path);
        }

        public static string ConcatPath(string path1, string path2, string path3)
        {
            return ConcatPath(ConcatPath(path1, path2), path3);
        }

        public static string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            path = UnifyPath(path).Replace(DirectorySeparatorChar, Path.DirectorySeparatorChar);

            var rootLength = (Path.GetPathRoot(path) ?? string.Empty).Length;
            var length = path.Length;

            while (length > rootLength && path[length - 1] == Path.DirectorySeparatorChar)
            {
                length--;
            }

            if (length <= rootLength)
            {
                return string.Empty;
            }

            path = path.Substring(0, length);
            return UnifyPath(Path.GetDirectoryName(path) ?? string.Empty);
        }

        public static string GetRelativePath(string rootPath, string fullPath)
        {
            return UnifyPath(Path.GetRelativePath(UnifyPath(rootPath), UnifyPath(fullPath)));
        }

        public static string GetRelativeAssetRootPath(string path)
        {
            path = UnifyPath(path).TrimEnd(DirectorySeparatorChar);
            var root = UnifyPath(LiteConst.AssetRootPath).TrimEnd(DirectorySeparatorChar);

            if (string.Equals(path, root, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            var prefix = root + DirectorySeparatorChar;
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring(prefix.Length);
            }

            return path;
        }

        public static string[] GetRelativeAssetRootPath(string[] pathList)
        {
            var result = new string[pathList.Length];

            for (var index = 0; index < pathList.Length; ++index)
            {
                result[index] = GetRelativeAssetRootPath(pathList[index]);
            }

            return result;
        }

        /// <summary>
        /// Get the full path in the standalone root.
        /// </summary>
        /// <returns>Assets/StandaloneAssets/{path}</returns>
        public static string GetFullPathInAssetRoot(string path)
        {
            if (path.StartsWith(LiteConst.AssetRootName, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return ConcatPath(LiteConst.AssetRootPath, path);
        }

        /// <summary>
        /// Get the full path in the asset data.
        /// </summary>
        /// <returns>Assets/{path}</returns>
        public static string GetAssetDataPath(string path)
        {
            return ConcatPath(Application.dataPath, path);
        }

        /// <summary>
        /// Get the full path in persistent root.
        /// </summary>
        public static string GetPersistentDataPath(string path)
        {
            return ConcatPath(Application.persistentDataPath, path);
        }

        /// <summary>
        /// Get the full path in persistent tag root.
        /// </summary>
        public static string GetPersistentDataPath(string tag, string path)
        {
            return ConcatPath(Application.persistentDataPath, tag, path);
        }

        /// <summary>
        /// Get the full path in streaming assets.
        /// </summary>
        public static string GetStreamingAssetsPath(string path)
        {
            return ConcatPath(Application.streamingAssetsPath, path);
        }

        /// <summary>
        /// Get the full path in streaming assets tag root.
        /// </summary>
        public static string GetStreamingAssetsPath(string tag, string path)
        {
            return ConcatPath(Application.streamingAssetsPath, tag, path);
        }

        public static string GetFullPathInRuntime(string path)
        {
            var fullPath = GetPersistentDataPath(LiteConst.Tag, path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            return GetStreamingAssetsPath(LiteConst.Tag, path);
        }

        public static bool RenameFile(string oldPath, string newPath)
        {
            if (!File.Exists(oldPath))
            {
                return false;
            }

            var newPathDir = GetParentPath(newPath);
            if (!string.IsNullOrEmpty(newPathDir))
            {
                Directory.CreateDirectory(newPathDir);
            }

            File.Move(oldPath, newPath);
            return true;
        }

        public static void DeleteFile(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            File.Delete(path);
        }

        public static string[] GetFileList(string path, Func<string, string> handler = null)
        {
            var fileList = Directory.GetFiles(path);
            fileList = fileList.Select(UnifyPath).ToArray();

            if (handler != null)
            {
                return fileList.Select(handler).ToArray();
            }

            return fileList;
        }

        public static string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }

        public static string GetFileNameWithoutExt(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        public static string GetFilePathWithoutExt(string path)
        {
            var directory = Path.GetDirectoryName(path);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            var pathWithoutExtension = ConcatPath(directory, fileNameWithoutExtension);
            return pathWithoutExtension;
        }

        public static void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            Directory.Delete(path, true);
        }

        public static string[] GetDirectoryList(string path, Func<string, string> handler = null)
        {
            var directoryList = Directory.GetDirectories(path);
            directoryList = directoryList.Select(UnifyPath).ToArray();

            if (handler != null)
            {
                directoryList = directoryList.Select(handler).ToArray();
            }

            return directoryList;
        }

        public static void CopyDirectory(string sourcePath, string destPath, Predicate<string> condition = null)
        {
            sourcePath = UnifyPath(sourcePath);
            destPath = UnifyPath(destPath);

            if (!Directory.Exists(sourcePath))
            {
                return;
            }

            // 防止复制到自身或源目录内部。
            var relativeDestPath = GetRelativePath(sourcePath, destPath);
            if (relativeDestPath == "." ||
                (!Path.IsPathRooted(relativeDestPath) &&
                 relativeDestPath != ".." &&
                 !relativeDestPath.StartsWith("../", StringComparison.Ordinal)))
            {
                throw new ArgumentException("Destination must not be the source directory or its child.", nameof(destPath));
            }

            foreach (var filePath in Directory.GetFiles(sourcePath))
            {
                if (condition != null && !condition.Invoke(filePath))
                {
                    continue;
                }

                Directory.CreateDirectory(destPath);

                var destFilePath = Path.Combine(destPath, Path.GetFileName(filePath));
                File.Copy(filePath, destFilePath, true);
            }

            foreach (var directoryPath in Directory.GetDirectories(sourcePath))
            {
                var destDirectoryPath = Path.Combine(destPath, Path.GetFileName(directoryPath));
                CopyDirectory(directoryPath, destDirectoryPath, condition);
            }
        }

#if UNITY_EDITOR
        public static string GetLiteQuarkRootPath(string subPath)
        {
            var projectPath = GetParentPath(Application.dataPath);
            return ConcatPath(projectPath, LiteConst.Tag, subPath);
        }
#endif
    }
}
