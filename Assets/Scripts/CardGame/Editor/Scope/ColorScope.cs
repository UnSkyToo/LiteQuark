using UnityEngine;

namespace LiteEditor
{
    public sealed class ColorScope : GUI.Scope
    {
        public Color Color { get; }

        public ColorScope(Color color)
        {
            Color = GUI.color;
            GUI.color = color;
        }

        protected override void CloseScope()
        {
            GUI.color = Color;
        }
    }
}