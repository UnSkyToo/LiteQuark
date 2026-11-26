using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal static class ValidationEnvironment
    {
        private static string Root => Directory.GetParent(Application.dataPath).Parent.FullName;

        internal static BuildTarget BundleTarget => Application.platform == RuntimePlatform.OSXEditor
            ? BuildTarget.StandaloneOSX : BuildTarget.StandaloneWindows64;

        internal static string Reports(string suite)
        {
            var path = Path.Combine(Root, "Reports", suite);
            Directory.CreateDirectory(path);
            return path;
        }

        internal static string[] ExpectedScenarios(string suite)
        {
            var catalog = JsonUtility.FromJson<ScenarioCatalog>(File.ReadAllText(Path.Combine(Root, "Scenarios.json")));
            return catalog.suites.Single(entry => entry.name == suite).scenarios ?? Array.Empty<string>();
        }

        [Serializable]
        private sealed class ScenarioCatalog
        {
            public Suite[] suites = Array.Empty<Suite>();
        }

        [Serializable]
        private sealed class Suite
        {
            public string name = string.Empty;
            public string[] scenarios = Array.Empty<string>();
        }
    }
}
