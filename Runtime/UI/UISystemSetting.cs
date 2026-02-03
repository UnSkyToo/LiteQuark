using System;
using UnityEngine;
using UnityEngine.UI;

namespace LiteQuark.Runtime
{
    [Serializable, LiteLabel("界面设置")]
    public class UISystemSetting : ISystemSetting
    {
        [SerializeField] public Camera UICamera;
        [SerializeField] public CanvasScaler.ScaleMode ScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        [SerializeField] public CanvasScaler.ScreenMatchMode MatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        [SerializeField] [Range(0, 1)] public float MatchValue = 0f;
        [SerializeField] public int ResolutionWidth = 1920;
        [SerializeField] public int ResolutionHeight = 1080;

        public UISystemSetting()
        {
        }
    }
}