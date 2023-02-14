using UnityEngine;

namespace LiteQuark.Runtime
{
    public interface IBezierCurve
    {
        Vector3 Lerp(float time);
    }
}