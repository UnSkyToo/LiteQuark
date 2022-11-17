using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        LogManager.Instance.Startup();
        // LogManager.Instance.Info("test info");
        // LogManager.Instance.Error("test error");
        //
        // LogManager.Instance.GetLogger("my logger").Fatal("my fatal");

        var logger = LogManager.Instance.GetLogger(typeof(Test));
        logger.EnableLevel(LogLevel.Warn, false);
        
        LogManager.Instance.EnableLevel(LogLevel.Error, false);
        
        // LogManager.Instance.GetRepository().EnableLevel(LogLevel.All, false);
        
        logger.Info("record info test");
        logger.Warn("record warn test");
        logger.Error("record error test");
        logger.Fatal("record fatal test");
        logger.Fatal("record fatal ex test", new ArgumentException("test arg null"));
        
        logger.EnableLevel(LogLevel.Warn, true);
        logger.Warn("record warn 2");
        
        LogManager.Instance.Info("c info");
        LogManager.Instance.Warn("c warn");
        LogManager.Instance.Error("c error");
        LogManager.Instance.Fatal("c fatal");

        // var threads = new List<Thread>();
        // for (var i = 0; i < 32; i++)
        // {
        //     var thread = new Thread(LogABit);
        //     thread.Start();
        //     threads.Add(thread);
        // }
        //
        // foreach (var t in threads)
        // {
        //     t.Join();
        // }
        //
        // void LogABit()
        // {
        //     for (var i = 0; i < 100; i++)
        //     {
        //         logger.Info($"thread<{Thread.CurrentThread.ManagedThreadId}> test log {i}");
        //         logger.Error($"thread<{Thread.CurrentThread.ManagedThreadId}> test error {i}");
        //     }
        // }
    }

    private void OnDestroy()
    {
        LogManager.Instance.Shutdown();
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(5, 5, 200, 120), "Test1"))
        {
            // AssetManager.Instance.LoadAsset<GameObject>("Prefab/Go1.prefab", (go) =>
            // {
            //     Instantiate(go);
            // });
            
            var text = new StringBuilder(10000);
            
            for (var i = 0; i < 10000; ++i)
            {
                text.Append("a");
            }
            
            text.Append("b");

            var t = new WriteFileAsyncTask("C://Data//t.txt", Encoding.Default.GetBytes(text.ToString()), (suc) =>
            {
                Debug.Log(suc);
            });
            t.Execute();
            Debug.Log("test1");
        }
        
        if (GUI.Button(new Rect(5, 205, 200, 120), "Test2"))
        {
            // AssetManager.Instance.LoadAsset<GameObject>("Prefab/Go2.prefab", (go) =>
            // {
            //     Instantiate(go);
            // });
            
            var t = new ReadFileAsyncTask("C://Data//t.txt", (suc) =>
            {
                Debug.Log(suc.Length);
            });
            t.Execute();
            Debug.Log("test2");
        }
    }
}
