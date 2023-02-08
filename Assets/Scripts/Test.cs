using System;
using LiteGamePlay.Chess;
using LiteQuark.Runtime;
using UnityEngine;

public class Test : MonoBehaviour
{
    void Start()
    {
        var a = typeof(ChessKind);
        var b = typeof(AssetLoaderMode);

        var a1 = a.ToString() + "," + a.Assembly.ToString();
        var b1 = b.ToString() + "," + b.Assembly.ToString();

        var az = Type.GetType(a1);
        var bz = Type.GetType(b1);
    }

    private void OnDestroy()
    {
    }

    void Update()
    {
    }
    
    private void OnGUI()
    {
    }
}