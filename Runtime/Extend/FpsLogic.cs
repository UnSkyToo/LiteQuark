using UnityEngine;

namespace LiteQuark.Runtime
{
    public class FpsLogic : ILogic, IOnGUI
    {
        public float X { get; set; }
        public float Y { get; set; }
        public Color TextColor { get; set; }
        
        private GUIStyle Style_;

        private float SampleRate_;
        private float Time_;
        private float Fps_;

        public bool Startup()
        {
            X = 5;
            Y = 5;
            TextColor = Color.green;

            Style_ = new GUIStyle();
            Style_.fontSize = 40;
            Style_.normal.background = null;
            Style_.normal.textColor = TextColor;

            SampleRate_ = 1.0f / 120.0f;
            Time_ = 0;
            Fps_ = 0;

            return true;
        }

        public void Shutdown()
        {
        }
        
        public void Tick(float deltaTime)
        {
            if (Time_ == 0)
            {
                Time_ = deltaTime;
            }
            else
            {
                Time_ = Time_ * (1 - SampleRate_) + deltaTime * SampleRate_;
            }

            Fps_ = 1.0f / Time_;
        }

        public void OnGUI()
        {
            GUI.Label(new Rect(X, Y, 100, 40), $"Fps:{Fps_:0.0}", Style_);
        }
    }
}