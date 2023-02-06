using UnityEngine;

public class Test : MonoBehaviour
{
    void Start()
    {
        var b = GameObject.Find("Y");
        var c = b.GetComponent<RectTransform>();
        
        var canvas = new GameObject("z");
        canvas.transform.SetParent(GameObject.Find("X").transform, false);
        var a = canvas.AddComponent<RectTransform>();
        a.anchorMin = Vector2.zero;
        a.anchorMax = Vector2.one;
        a.anchoredPosition = Vector2.zero;
        a.sizeDelta = Vector2.zero;
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