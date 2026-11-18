using System.IO;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal static class ValidationEnvironment
    {
        internal static BuildTarget BundleTarget => Application.platform == RuntimePlatform.OSXEditor
            ? BuildTarget.StandaloneOSX : BuildTarget.StandaloneWindows64;

        internal static string Reports(string suite)
        {
            var root = Directory.GetParent(Application.dataPath).Parent.FullName;
            var path = Path.Combine(root, "Reports", suite);
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
