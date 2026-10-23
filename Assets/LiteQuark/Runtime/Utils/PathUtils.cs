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

        public static string ConcatPath(string path1, string path2)
        {
            return UnifyPath(Path.Combine(path1, path2));
        }

        public static string ConcatPath(string path1, string path2, string path3)
        {
            return UnifyPath(Path.Combine(path1, path2, path3));
        }

        public static string ConcatUri(string baseUri, string path)
        {
            var builder = new UriBuilder(new Uri(baseUri, UriKind.Absolute));
            var relativePath = UnifyPath(path).Trim(DirectorySeparatorChar);

            if (relativePath.Length > 0)
            {
                var escapedPath = string.Join(DirectorySeparatorChar, relativePath.Split(DirectorySeparatorChar).Select(Uri.EscapeDataString));
                builder.Path = builder.Path.TrimEnd(DirectorySeparatorChar) + DirectorySeparatorChar + escapedPath;
            }

            return builder.Uri.AbsoluteUri;
        }

        public static string ConcatUri(string baseUri, string path1, string path2)
        {
            return ConcatUri(ConcatUri(baseUri, path1), path2);
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
            var root = Application.streamingAssetsPath;
            return root.IndexOf("://", StringComparison.Ordinal) >= 0 ? ConcatUri(root, path) : ConcatPath(root, path);
        }

        /// <summary>
        /// Get the full path in streaming assets tag root.
        /// </summary>
        public static string GetStreamingAssetsPath(string tag, string path)
        {
            var root = Application.streamingAssetsPath;

            return root.IndexOf("://", StringComparison.Ordinal) >= 0 ? ConcatUri(root, tag, path) : ConcatPath(root, tag, path);
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
    }
}
