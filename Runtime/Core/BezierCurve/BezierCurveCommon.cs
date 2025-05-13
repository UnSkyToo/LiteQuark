using UnityEngine;

namespace LiteQuark.Runtime
{
    public class BezierCurveCommon : IBezierCurve
    {
        private readonly EaseKind EaseKind_;
        private readonly Vector3[] Points_;
        private readonly Vector3[] Controller_;

        public BezierCurveCommon(EaseKind easeKind, Vector3[] points)
        {
            EaseKind_ = easeKind;

            if (points != null && points.Length > 1)
            {
                Points_ = points;
                Controller_ = new Vector3[points.Length];

                for (var index = 0; index < Controller_.Length; ++index)
                {
                    Controller_[index] = new Vector3();
                }
            }
        }

        public Vector3 Lerp(float time)
        {
            if (Points_ == null)
            {
                return Vector3.zero;
            }

            var modulateTime = EaseUtils.Sample(EaseKind_, time);
            var count = GeneratorController(Points_, Points_.Length, modulateTime);
            while (count > 1)
            {
                count = GeneratorController(Controller_, count, time);
            }

            return Controller_[0];
        }

        private int GeneratorController(Vector3[] points, int count, float time)
        {
            var index = 0;
            for (var offset = 0; offset < count - 1; ++offset)
            {
                Controller_[index++] = Vector3.Lerp(points[offset], points[offset + 1], time);
            }

            return index;
        }
    }
}