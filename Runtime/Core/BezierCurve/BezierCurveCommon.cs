using UnityEngine;

namespace LiteQuark.Runtime
{
    public class BezierCurveCommon : IBezierCurve
    {
        private readonly IBezierCurveModulator Modulator_;
        private readonly Vector3[] Points_;
        private readonly Vector3[] Controller_;

        public BezierCurveCommon(IBezierCurveModulator modulator, Vector3[] points)
        {
            Modulator_ = modulator;

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

            var modulateTime = Modulator_?.Modulation(time) ?? time;
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
                Controller_[index].x = (points[offset + 1].x - points[offset].x) * time + points[offset].x;
                Controller_[index].y = (points[offset + 1].y - points[offset].y) * time + points[offset].y;
                Controller_[index].z = (points[offset + 1].z - points[offset].z) * time + points[offset].z;
                index++;
            }

            return index;
        }
    }
}