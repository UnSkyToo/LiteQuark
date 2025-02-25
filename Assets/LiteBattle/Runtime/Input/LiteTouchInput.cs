using UnityEngine;

namespace LiteBattle.Runtime
{
    public sealed class LiteTouchInput
    {
        public bool Enable { get; set; } = true;
        
        private readonly System.Action<Vector2> OnTouchMove_;
        private readonly System.Action<float> OnTouchScale_;
        
        private Vector2 PreviousTouchPosition1_;
        private Vector2 PreviousTouchPosition2_;

        private Vector2 LastMousePosition_;


        public LiteTouchInput(System.Action<Vector2> onTouchMove, System.Action<float> onTouchScale)
        {
            OnTouchMove_ = onTouchMove;
            OnTouchScale_ = onTouchScale;
        }

        public void Update(float deltaTime)
        {
            if (!Enable)
            {
                return;
            }
            
#if (((UNITY_ANDROID || UNITY_IPHONE || UNITY_WP8 || UNITY_BLACKBERRY) && !UNITY_EDITOR))
            UpdateTouch(deltaTime);
#else
            UpdateMouse(deltaTime);
#endif
        }

        private void UpdateTouch(float deltaTime)
        {
            if (Input.touchCount < 1)
            {
                return;
            }

            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Ended)
                {
                    OnTouchMove_?.Invoke(touch.deltaPosition);
                }
            }
            else if (Input.touchCount > 1)
            {
                var touch1 = Input.GetTouch(0);
                var touch2 = Input.GetTouch(1);

                if (touch2.phase == TouchPhase.Began)
                {
                    PreviousTouchPosition1_ = touch1.position;
                    PreviousTouchPosition2_ = touch2.position;
                }
                else
                {
                    var previousDist = Vector2.Distance(PreviousTouchPosition1_, PreviousTouchPosition2_);
                    var currentDist = Vector2.Distance(touch1.position, touch2.position);
                    var scale = currentDist - previousDist;
                    OnTouchScale_?.Invoke(scale);
                }
            }
        }

        private void UpdateMouse(float deltaTime)
        {
            if (Input.GetMouseButtonDown(0))
            {
                LastMousePosition_ = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
            {
                Vector2 offset = Input.mousePosition;
                offset -= LastMousePosition_;
                LastMousePosition_ = Input.mousePosition;
                OnTouchMove_?.Invoke(offset);
            }

            OnTouchScale_?.Invoke(Input.mouseScrollDelta.y * 10);
        }
    }
}