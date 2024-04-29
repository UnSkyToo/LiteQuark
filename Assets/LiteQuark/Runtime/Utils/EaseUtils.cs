namespace LiteQuark.Runtime
{
    public enum EaseKind
    {
        Linear,
        InBack,
    }
    
    public static class EaseUtils
    {
        public static float Sample(EaseKind kind, float t)
        {
            switch (kind)
            {
                case EaseKind.Linear:
                    return Linear(t);
                case EaseKind.InBack:
                    return InBack(t);
            }

            return t;
        }

        private static float Linear(float t)
        {
            return t;
        }
        
        private static float InBack(float t)
        {
            const float s = 1.70158f; // 定义一个超调参数
            return t * t * ((s + 1) * t - s); // 根据三次方程的公式计算值
        }
    }
}