using UnityEngine;

namespace LiteQuark.Runtime
{
    public interface IAlphaBox
    {
        void SetAlpha(float alpha);
    }
    
    public class AlphaBox : IAlphaBox
    {
        private readonly SpriteRenderer[] Renderers_;
        private readonly CanvasGroup[] CanvasGroups_;
        private readonly Color[] Colors_;

        public AlphaBox(Transform transform)
        {
            Renderers_ = transform.GetComponentsInChildren<SpriteRenderer>();
            Colors_ = new Color[Renderers_.Length];

            for (var i = 0; i < Renderers_.Length; ++i)
            {
                Colors_[i] = Renderers_[i].color;
            }

            CanvasGroups_ = transform.GetComponentsInChildren<CanvasGroup>();
        }

        public void SetAlpha(float alpha)
        {
            for (var i = 0; i < Renderers_.Length; ++i)
            {
                Colors_[i].a = alpha;
                Renderers_[i].color = Colors_[i];
            }

            foreach (var group in CanvasGroups_)
            {
                group.alpha = alpha;
            }
        }
    }
}