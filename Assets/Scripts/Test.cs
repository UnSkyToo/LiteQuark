using System;
using System.Collections;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEditor;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnDestroy()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(5, 5, 200, 120), "Create1"))
        {
            AssetManager.Instance.LoadAsset<GameObject>("Prefab/Go1.prefab", (go) =>
            {
                Instantiate(go);
            });
        }
        
        if (GUI.Button(new Rect(5, 205, 200, 120), "Create2"))
        {
            AssetManager.Instance.LoadAsset<GameObject>("Prefab/Go2.prefab", (go) =>
            {
                Instantiate(go);
            });
        }
    }
}
