using LiteQuark.Runtime;
using Platform;
using UnityEngine;

public class Test : MonoBehaviour
{
    public GameObject Instance;

    private GameObject[] GoList_ = new GameObject[50000];

    private GameObjectPool Pool_;

    void Start()
    {
        Pool_ = LiteRuntime.Get<ObjectPoolSystem>().GetPool<GameObjectPool>("Test/Prefab/CircleTest.prefab");
    }

    private void OnDestroy()
    {
    }

    void Update()
    {
        InputMgr.Instance.Update();

        if (Input.GetMouseButtonDown(0))
        {
            Show();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            Hide();
        }
    }

    private void Show()
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        for (var i = 0; i < GoList_.Length; ++i)
        {
            GoList_[i] = Pool_.Alloc();
        }

        sw.Stop();
        Debug.LogWarning($"Show time : {sw.Elapsed.TotalSeconds}");
    }

    private void Hide()
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        for (var i = 0; i < GoList_.Length; ++i)
        {
            Pool_.Recycle(GoList_[i]);
        }

        sw.Stop();
        Debug.LogWarning($"Hide time : {sw.Elapsed.TotalSeconds}");
    }

    private void Show2()
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        for (var i = 0; i < GoList_.Length; ++i)
        {
            GoList_[i].transform.localPosition = new Vector3(0, 0, 0);
        }

        sw.Stop();
        Debug.LogWarning($"Show2 time : {sw.Elapsed.TotalSeconds}");
    }

    private void Hide2()
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        for (var i = 0; i < GoList_.Length; ++i)
        {
            GoList_[i].transform.localPosition = new Vector3(10000, 10000, 10000);
        }

        sw.Stop();
        Debug.LogWarning($"Hide2 time : {sw.Elapsed.TotalSeconds}");
    }
}