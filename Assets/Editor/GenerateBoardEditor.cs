using UnityEditor;
using UnityEngine;

namespace LiteGamePlay.Editor
{
    // public class GenerateBoardEditor : EditorWindow
    // {
    //     [MenuItem("Lite/Generate Board")]
    //     private static void ShowWin()
    //     {
    //         var win = GetWindow<GenerateBoardEditor>();
    //         win.Show();
    //     }
    //
    //     private GameObject Go_;
    //     
    //     private void OnGUI()
    //     {
    //         Go_ = EditorGUILayout.ObjectField(new GUIContent("Prefab"), Go_, typeof(GameObject), true) as GameObject;
    //         if (Go_ != null)
    //         {
    //             if (GUILayout.Button("Generate"))
    //             {
    //                 var step = 7.02f / 18f;
    //                 
    //                 var hParent = Go_.transform.Find("Horizontal");
    //                 var h1 = hParent.Find("L1").gameObject;
    //
    //                 var y = 3.51f;
    //                 
    //                 for (var i = 2; i < 18; ++i)
    //                 {
    //                     var hInstance = Instantiate(h1);
    //                     hInstance.name = $"L{i}";
    //                     hInstance.transform.SetParent(hParent, false);
    //                     hInstance.transform.localPosition = new Vector3(0, y - i * step, 0);
    //                 }
    //                 
    //                 var vParent = Go_.transform.Find("Vertical");
    //                 var v1 = vParent.Find("L1").gameObject;
    //
    //                 var x = -3.51f;
    //                 
    //                 for (var i = 2; i < 18; ++i)
    //                 {
    //                     var vInstance = Instantiate(v1);
    //                     vInstance.name = $"L{i}";
    //                     vInstance.transform.SetParent(vParent, false);
    //                     vInstance.transform.localPosition = new Vector3(x + i * step, 0, 0);
    //                 }
    //             }
    //         }
    //     }
    // }
}