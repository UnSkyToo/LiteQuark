using System.Collections;
using System.Collections.Generic;
using LiteQuark.Runtime;
using UnityEngine;

public class GestureMgr : MonoBehaviour
{
    private Vector2 BeginPos_;
    private Vector2 EndPos_;

    private const float MinDistance = 80;

    public enum GestureDirection
    {
        None,
        Left,
        Right,
        Up,
        Down,
    }
    
    void Start()
    {
    }
    
    void Update()
    {
        if (Input.touchCount != 1)
        {
            return;
        }

        var touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                BeginPos_ = touch.position;
                break;
            case TouchPhase.Ended:
                EndPos_ = touch.position;
                Dispatch();
                break;
        }
    }

    private void Dispatch()
    {
        var distance = Vector2.Distance(BeginPos_, EndPos_);
        if (distance < MinDistance)
        {
            return;
        }
        
        var angle = MathUtils.AngleByPoint(BeginPos_, EndPos_);
        // 315 - 45
        // 45 - 135
        // 135 - 225
        // 225 - 315
        var dir = GestureDirection.None;
        if (angle is >= 45 and < 135)
        {
            dir = GestureDirection.Right;
        }
        else if (angle is >= 135 and < 225)
        {
            dir = GestureDirection.Down;
        }
        else if (angle is >= 225 and < 315)
        {
            dir = GestureDirection.Left;
        }
        else
        {
            dir = GestureDirection.Up;
        }
        
        Debug.LogError(dir);
    }
}
