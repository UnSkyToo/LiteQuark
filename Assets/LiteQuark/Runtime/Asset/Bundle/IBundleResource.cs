using System;
using UnityEngine.SceneManagement;

namespace LiteQuark.Runtime
{
    // 已加载的内容容器。交给 Cache 后由它独占释放；观察结果和创建读取任务不取得所有权。
    internal interface IBundleResource : IDispose
    {
        string[] GetAllScenePaths();
        long GetRuntimeMemorySize();

        // 只构造任务；调用方提交到 TaskSystem，并持有 Bundle Lease 直到读取收尾。
        BaseTask CreateLoadAssetTask<T>(string assetKey, Action<ITask> callback) where T : UnityEngine.Object;
        BaseTask CreateLoadSceneTask(string loadPath, LoadSceneParameters parameters, Action<ITask> callback);
    }
}
