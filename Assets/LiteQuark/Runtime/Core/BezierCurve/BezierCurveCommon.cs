using UnityEngine;

namespace LiteQuark.Runtime
{
    public class BezierCurveCommon : IBezierCurve
    {
        private readonly EaseKind _easeKind;
        private readonly Vector3[] _points;
        private readonly Vector3[] _controller;

        public BezierCurveCommon(EaseKind easeKind, Vector3[] points)
        {
            _easeKind = easeKind;

            if (points != null && points.Length > 1)
            {
                _points = points;
                _controller = new Vector3[points.Length];

                for (var index = 0; index < _controller.Length; ++index)
                {
                    _controller[index] = new Vector3();
                }
            }
        }

        public Vector3 Lerp(float time)
        {
            if (_points == null)
            {
                return Vector3.zero;
            }

            var modulateTime = EaseUtils.Sample(_easeKind, time);
            var count = GeneratorController(_points, _points.Length, modulateTime);
            while (count > 1)
            {
                count = GeneratorController(_controller, count, time);
            }

            return _controller[0];
        }

        private int GeneratorController(Vector3[] points, int count, float time)
        {
            var index = 0;
            for (var offset = 0; offset < count - 1; ++offset)
            {
                _controller[index++] = Vector3.Lerp(points[offset], points[offset + 1], time);
            }

            return index;
        }
    }
}