using UnityEngine;
using UnityEngine.UI;

namespace LiteQuark.Runtime
{
    public interface IAlphaBox
    {
        void SetAlpha(float alpha);
    }
    
    public class AlphaBox : IAlphaBox
    {
        private readonly SpriteRenderer[] _renderers;
        private readonly Color[] _renderColors;
        private readonly Graphic[] _graphics;
        private readonly Color[] _graphicColors;
        private readonly CanvasGroup[] _canvasGroups;

        public AlphaBox(Transform transform)
        {
            _renderers = transform.GetComponentsInChildren<SpriteRenderer>();
            _renderColors = new Color[_renderers.Length];
            for (var i = 0; i < _renderers.Length; ++i)
            {
                _renderColors[i] = _renderers[i].color;
            }
            
            _graphics = transform.GetComponentsInChildren<Graphic>();
            _graphicColors = new Color[_graphics.Length];
            for (var i = 0; i < _graphics.Length; ++i)
            {
                _graphicColors[i] = _graphics[i].color;
            }
            
            _canvasGroups = transform.GetComponentsInChildren<CanvasGroup>();
        }

        public void SetAlpha(float alpha)
        {
            for (var i = 0; i < _renderers.Length; ++i)
            {
                _renderColors[i].a = alpha;
                _renderers[i].color = _renderColors[i];
            }
            
            for (var i = 0; i < _graphics.Length; ++i)
            {
                _graphicColors[i].a = alpha;
                _graphics[i].color = _graphicColors[i];
            }

            foreach (var group in _canvasGroups)
            {
                group.alpha = alpha;
            }
        }
    }
}