namespace LiteQuark.Runtime
{
    internal class BuiltInBundleProvider : IBundleProvider
    {
        public bool EditorForceStreamingAssets { get; set; } = true;

        public BuiltInBundleProvider()
        {
        }

        private string GetRuntimeFullPath(string path)
        {
#if UNITY_EDITOR
            if (EditorForceStreamingAssets)
            {
                return PathUtils.GetStreamingAssetsPath(LiteConst.Tag, path);
            }
#endif
            var persistentPath = PathUtils.GetPersistentDataPath(LiteConst.Tag, path);
            if (System.IO.File.Exists(persistentPath))
            {
                return persistentPath;
            }

            return PathUtils.GetStreamingAssetsPath(LiteConst.Tag, path);
        }

        public BaseTask LoadVersionPack(string versionFileName)
        {
            var versionPackUri = GetRuntimeFullPath(versionFileName);
            var task = new LoadVersionPackTask(versionPackUri, null);
            LiteRuntime.Task.AddTask(task, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
            return task;
        }

        public ILoadBundleTask LoadBundle(string bundlePath, string hash)
        {
            var bundleUri = GetRuntimeFullPath(bundlePath);
            var task = new LoadLocalBundleTask(bundleUri);
            LiteRuntime.Task.AddTask(task, LiteConst.TaskGroupId.Asset, TaskPriority.Urgent);
            return task;
        }
    }
}
