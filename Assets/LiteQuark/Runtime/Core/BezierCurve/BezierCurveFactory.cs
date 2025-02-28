using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class BezierCurveFactory
    {
        public static IBezierCurve CreateBezierCurve(Vector3 begin, Vector3 end, BezierCurveModulateMode mode = BezierCurveModulateMode.None)
        {
            return new BezierCurveCommon(CreateModulator(mode), new[] {begin, end});
        }

        public static IBezierCurve CreateBezierCurve(Vector3 begin, Vector3 control, Vector3 end, BezierCurveModulateMode mode = BezierCurveModulateMode.None)
        {
            return new BezierCurveCommon(CreateModulator(mode), new[] {begin, control, end});
        }

        public static IBezierCurve CreateBezierCurve(Vector3 begin, Vector3 control1, Vector3 control2, Vector3 end, BezierCurveModulateMode mode = BezierCurveModulateMode.None)
        {
            return new BezierCurveCommon(CreateModulator(mode), new[] {begin, control1, control2, end});
        }

        public static IBezierCurve CreateBezierCurve(Vector3[] points, BezierCurveModulateMode mode = BezierCurveModulateMode.None)
        {
            return new BezierCurveCommon(CreateModulator(mode), points);
        }
		
        private static IBezierCurveModulator CreateModulator(BezierCurveModulateMode mode)
        {
            switch (mode)
            {
                case BezierCurveModulateMode.None:
                    return null;
                case BezierCurveModulateMode.In:
                    return new BezierCurveModulatorIn();
                case BezierCurveModulateMode.Out:
                    return new BezierCurveModulatorOut();
                case BezierCurveModulateMode.InOut:
                    return new BezierCurveModulatorInOut();
                default:
                    return null;
            }
        }
    }
}