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
            if (File.Exists(filePath))
            {
                return true;
            }
            
            return filePath.LastIndexOf('.') > filePath.LastIndexOf('/');
        }
        
        public static string UnifyPath(string path)
        {
            var unifyPath = path.Replace('\\', DirectorySeparatorChar);

            if (PathIsFile(path))
            {
                return unifyPath;
            }

            return unifyPath.TrimEnd(DirectorySeparatorChar);
        }

        public static string ConcatPath(string path1, string path2)
        {
            var path = Path.Combine(path1, path2);
            return UnifyPath(path);
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

        public static string GetFullPathInAssetRoot(string path)
        {
            return ConcatPath(LiteConst.AssetRootPath, path);
        }
        
        public static string GetAssetDataPath(string path)
        {
            return $"{Application.dataPath}/{path}";
        }

        public static string GetPersistentDataPath(string path)
        {
            return $"{Application.persistentDataPath}/{path}";
        }

        public static string GetStreamingAssetsPath(string path)
        {
            return $"{Application.streamingAssetsPath}/{path}";
        }
        
        public static string GetFullPathInRuntime(string path)
        {
            var fullPath = GetPersistentDataPath(path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            return GetStreamingAssetsPath(path);
        }

        public static string GetPathFromFullPath(string fullPath)
        {
            var path = PathIsFile(fullPath) ? Path.GetDirectoryName(fullPath) : fullPath;
            return UnifyPath(path);
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

        public static string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }
        
        public static void CreateDirectory(string path)
        {
            if (PathIsFile(path))
            {
                path = GetDirectoryName(path);
            }
            
            if (!Directory.Exists(path))
            {
                if (!Directory.CreateDirectory(path).Exists)
                {
                    LLog.Warning($"can't create directory : {path}");
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
                    var destFullPath = $"{destPath}{relativePath}";
                    
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
                var destFullPath = $"{destPath}{relativePath}";

                if (condition == null || condition.Invoke(directoryPath))
                {
                    CopyDirectory(directoryPath, destFullPath, condition);
                }
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