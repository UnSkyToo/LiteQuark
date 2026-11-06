#if UNITY_EDITOR
using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    internal sealed class LoadEditorSceneTask : LoadSceneTask
    {
        public LoadEditorSceneTask(string scenePath, LoadSceneParameters parameters, Action<ITask> callback)
            : base(scenePath, parameters, callback)
        {
        }

        protected override AsyncOperation StartLoadScene(string path, LoadSceneParameters parameters)
        {
            return EditorSceneManager.LoadSceneAsyncInPlayMode(path, parameters);
        }
    }
}
#endif
