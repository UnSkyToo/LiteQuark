using UnityEngine;

namespace LiteQuark.Runtime
{
    public class MotionAlphaBox
    {
        private readonly SpriteRenderer Renderer_;
        private readonly CanvasGroup Group_;
        private Color OriginColor_;

        public MotionAlphaBox(Transform master)
        {
            Renderer_ = master.GetComponent<SpriteRenderer>();
            if (Renderer_ == null)
            {
                Group_ = master.GetOrAddComponent<CanvasGroup>();
            }
            else
            {
                OriginColor_ = Renderer_.color;
            }
        }

        public void SetAlpha(float alpha)
        {
            if (Renderer_ != null)
            {
                OriginColor_.a = alpha;
                Renderer_.color = OriginColor_;
            }
            else
            {
                Group_.alpha = alpha;
            }
        }
    }
}