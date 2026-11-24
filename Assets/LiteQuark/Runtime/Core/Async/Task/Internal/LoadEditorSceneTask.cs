#if UNITY_EDITOR
using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    internal sealed class LoadEditorSceneTask : LoadSceneTask
    {
        public LoadEditorSceneTask(string scenePath, Action<LoadSceneTask> callback)
            : base(scenePath, callback)
        {
        }

        protected override AsyncOperation StartLoadScene(string path)
        {
            return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
        }
    }
}
#endif
