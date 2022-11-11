using UnityEditor;
using UnityEngine;

namespace LiteQuark.Editor
{
    public static class QuickNavigateMenu
    {
        [MenuItem("Lite/Navigate/Persistent Data Path", false, 1)]
        private static void OpenPersistentDataPath()
        {
            OpenFolder(Application.persistentDataPath);
        }

        [MenuItem("Lite/Navigate/Streaming Assets Path", false, 2)]
        private static void OpenStreamingAssetsPath()
        {
            OpenFolder(Application.streamingAssetsPath);
        }

        [MenuItem("Lite/Navigate/Data Path", false, 3)]
        private static void OpenDataPath()
        {
            OpenFolder(Application.dataPath);
        }

        [MenuItem("Lite/Navigate/Temporary Cache Path", false, 4)]
        private static void OpenTemporaryCachePath()
        {
            OpenFolder(Application.temporaryCachePath);
        }

        [MenuItem("Lite/Navigate/Console Log Path", false, 5)]
        private static void OpenConsoleLogPath()
        {
            OpenFolder(Application.consoleLogPath);
        }

        private static void OpenFolder(string path)
        {
#if UNITY_EDITOR_WIN
            Application.OpenURL(path);
#elif UNITY_STANDALONE_OSX
            EditorUtility.RevealInFinder(path);
#endif
        }
    }
}