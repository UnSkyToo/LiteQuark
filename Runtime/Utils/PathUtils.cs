using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class PathUtils
    {
        public const char DirectorySeparatorChar = '/';
        
        public static bool PathIsFile(string filePath)
        {
            return filePath.LastIndexOf('.') > filePath.LastIndexOf('/');
        }
        
        public static string UnifyPath(string path)
        {
            var unifyPath = path.Replace('\\', DirectorySeparatorChar);
            return unifyPath.TrimEnd(DirectorySeparatorChar);
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
            var index = path.LastIndexOf(DirectorySeparatorChar);
            return index > 0 ? path.Substring(0, index) : string.Empty;
        }

        public static string GetRelativePath(string rootPath, string fullPath)
        {
            var path = fullPath.Replace(rootPath, string.Empty).TrimStart(DirectorySeparatorChar);
            return UnifyPath(path);
        }

        public static string GetRelativeAssetRootPath(string path)
        {
            return GetRelativePath(LiteConst.AssetRootPath, path);
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

        public static string GetPathFromFullPath(string fullPath)
        {
            var path = PathIsFile(fullPath) ? Path.GetDirectoryName(fullPath) : fullPath;
            return UnifyPath(path);
        }
        
        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public static bool RenameFile(string oldPath, string newPath)
        {
            if (!File.Exists(oldPath))
            {
                return false;
            }

            var newPathDir = Path.GetDirectoryName(newPath);
            if (!Directory.Exists(newPathDir))
            {
                CreateDirectory(newPathDir);
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

        public static void CreateDirectory(string path)
        {
            if (PathIsFile(path))
            {
                path = Path.GetDirectoryName(path);
            }
            
            if (!Directory.Exists(path))
            {
                if (!Directory.CreateDirectory(path).Exists)
                {
                    LLog.Warning("Can't create directory : {0}", path);
                }
            }
        }

        public static void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }
            
            Directory.Delete(path, true);
        }

        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
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
            
            if (!DirectoryExists(sourcePath))
            {
                return;
            }
            
            var filePathList = Directory.GetFiles(sourcePath);
            var directoryPathList = Directory.GetDirectories(sourcePath);
            
            if (filePathList.Length > 0)
            {
                var isEmptyFolder = true;
                CreateDirectory(destPath);
                
                foreach (var filePath in filePathList)
                {
                    var relativePath = UnifyPath(filePath).Replace(sourcePath, string.Empty);
                    var destFullPath = ConcatPath(destPath, relativePath);
                    
                    if (condition == null || condition.Invoke(filePath))
                    {
                        isEmptyFolder = false;
                        File.Copy(filePath, destFullPath, true);
                    }
                }

                if (isEmptyFolder)
                {
                    DeleteDirectory(destPath);
                }
            }

            foreach (var directoryPath in directoryPathList)
            {
                var relativePath = UnifyPath(directoryPath).Replace(sourcePath, string.Empty);
                var destFullPath = ConcatPath(destPath, relativePath);
                
                CopyDirectory(directoryPath, destFullPath, condition);
            }
        }

#if UNITY_EDITOR
        public static string GetLiteQuarkRootPath(string subPath)
        {
            var dataPath = UnifyPath(Application.dataPath);
            dataPath = dataPath.Replace($"{DirectorySeparatorChar}Assets", string.Empty);
            return $"{dataPath}{DirectorySeparatorChar}LiteQuark{DirectorySeparatorChar}{subPath}";
        }
#endif
    }
}