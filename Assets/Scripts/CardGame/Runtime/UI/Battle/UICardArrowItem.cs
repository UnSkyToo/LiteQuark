using LiteQuark.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LiteCard.UI
{
    public class UICardArrowItem : MonoBehaviour
    {
        public Color Color = Color.red;
        public int NodeCount = 19;
        public float MinScale = 0.6f;
        public float MaxScale = 1.0f;
        public Vector2 Control1 = new Vector2(-100f, 100f);
        public Vector2 Control2 = new Vector2(100f, 300f);

        private RectTransform[] NodeList_;
        
        private void Start()
        {
            NodeList_ = new RectTransform[NodeCount];
            LiteRuntime.Get<AssetSystem>().LoadAsset<GameObject>("CardGame/Prefab/UI/Battle/ArrowBodyItem.prefab", (go) =>
            {
                for (var index = 0; index < NodeCount - 1; ++index)
                {
                    var instance = Instantiate(go);
                    UnityUtils.SetParent(transform, instance.transform);
                    instance.GetComponent<Image>().color = Color;
                    NodeList_[index] = instance.GetComponent<RectTransform>();
                }
            });
            
            LiteRuntime.Get<AssetSystem>().LoadGameObject("CardGame/Prefab/UI/Battle/ArrowHeadItem.prefab", (go) =>
            {
                UnityUtils.SetParent(transform, go.transform);
                go.GetComponent<Image>().color = Color;
                NodeList_[NodeCount - 1] = go.GetComponent<RectTransform>();
            });
        }

        // private Vector2 beginPos_ = new Vector2(0, 0);
        // private Vector2 endPos_ = Vector2.zero;
        // public void Update()
        // {
        //     if (Input.GetMouseButtonDown(0))
        //     {
        //         endPos_ = UIUtils.ScreenPosToCanvasPos(transform, Input.mousePosition);
        //         UpdatePosition(beginPos_, endPos_);
        //     }
        // }

        public void UpdatePosition(Vector2 beginPos, Vector2 endPos)
        {
            Debug.LogWarning(beginPos + "," + endPos);
            var lastPosition = beginPos;
            var ctrlPos1 = beginPos + Control1;
            var ctrlPos2 = beginPos + Control2;
            var positionBezier = BezierCurveFactory.CreateBezierCurve(beginPos, ctrlPos1, ctrlPos2, endPos);
            for (var index = 0; index < NodeCount; ++index)
            {
                var step = (float)(index + 1) / (float)NodeCount;
                var position = positionBezier.Lerp(step);
                NodeList_[index].anchoredPosition = positionBezier.Lerp(step);
                var angle = MathUtils.AngleByPoint(lastPosition, position);
                lastPosition = position;
                NodeList_[index].localRotation = Quaternion.AngleAxis(angle, Vector3.back);
                NodeList_[index].localScale = Vector3.one * Mathf.Lerp(MinScale, MaxScale, step);
            }
        }
    }
}