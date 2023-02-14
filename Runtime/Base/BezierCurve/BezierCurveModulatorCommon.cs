using UnityEngine;

namespace LiteQuark.Runtime
{
    public enum BezierCurveModulateMode
    {
        None,
        In,
        Out,
        InOut,
    }

    public class BezierCurveModulatorIn : IBezierCurveModulator
    {
        public float Modulation(float time)
        {
            return time * time;
        }
    }

    public class BezierCurveModulatorOut : IBezierCurveModulator
    {
        public float Modulation(float time)
        {
            //return Mathf.Sqrt(Time);
            return Mathf.Pow(time, 0.85f);
        }
    }

    public class BezierCurveModulatorInOut : IBezierCurveModulator
    {
        public float Modulation(float time)
        {
            return ((time * time * time) + (float)System.Math.Pow(time, 0.33334f)) / 2.0f;
        }
    }
}