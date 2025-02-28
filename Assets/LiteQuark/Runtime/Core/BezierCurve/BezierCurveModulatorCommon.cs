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
            return 1 - (1 - time) * (1 - time);
        }
    }

    public class BezierCurveModulatorInOut : IBezierCurveModulator
    {
        public float Modulation(float time)
        {
            if (time < 0.5f)
            {
                return 0.5f * (2 * time) * (2 * time);
            }
            else
            {
                return 0.5f * (2 * time - 2) * (2 * time - 2) + 1;
            }
        }
    }
}