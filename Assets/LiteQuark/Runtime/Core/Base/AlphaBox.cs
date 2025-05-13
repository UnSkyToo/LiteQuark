using UnityEngine;

namespace LiteQuark.Runtime
{
    public interface IAlphaBox
    {
        void SetAlpha(float alpha);
    }
    
    public class AlphaBox : IAlphaBox
    {
        private readonly SpriteRenderer[] _renderers;
        private readonly CanvasGroup[] _canvasGroups;
        private readonly Color[] _colors;

        public AlphaBox(Transform transform)
        {
            _renderers = transform.GetComponentsInChildren<SpriteRenderer>();
            _colors = new Color[_renderers.Length];

            for (var i = 0; i < _renderers.Length; ++i)
            {
                _colors[i] = _renderers[i].color;
            }

            _canvasGroups = transform.GetComponentsInChildren<CanvasGroup>();
        }

        public void SetAlpha(float alpha)
        {
            for (var i = 0; i < _renderers.Length; ++i)
            {
                _colors[i].a = alpha;
                _renderers[i].color = _colors[i];
            }

            foreach (var group in _canvasGroups)
            {
                group.alpha = alpha;
            }
        }
    }
}