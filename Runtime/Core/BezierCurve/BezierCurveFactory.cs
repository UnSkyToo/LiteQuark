using UnityEngine;

namespace LiteQuark.Runtime
{
    public static class BezierCurveFactory
    {
        public static IBezierCurve CreateBezierCurve(Vector3 begin, Vector3 end, EaseKind easeKind = EaseKind.Linear)
        {
            return new BezierCurveCommon(easeKind, new[] {begin, end});
        }

        public static IBezierCurve CreateBezierCurve(Vector3 begin, Vector3 control, Vector3 end, EaseKind easeKind = EaseKind.Linear)
        {
            return new BezierCurveCommon(easeKind, new[] {begin, control, end});
        }

        public static IBezierCurve CreateBezierCurve(Vector3 begin, Vector3 control1, Vector3 control2, Vector3 end, EaseKind easeKind = EaseKind.Linear)
        {
            return new BezierCurveCommon(easeKind, new[] {begin, control1, control2, end});
        }

        public static IBezierCurve CreateBezierCurve(Vector3[] points, EaseKind easeKind = EaseKind.Linear)
        {
            return new BezierCurveCommon(easeKind, points);
        }
    }
}