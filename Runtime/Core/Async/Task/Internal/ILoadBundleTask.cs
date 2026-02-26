using UnityEngine;

namespace LiteQuark.Runtime
{
    public interface ILoadBundleTask : ITask
    {
        AssetBundle GetBundle();
    }
}