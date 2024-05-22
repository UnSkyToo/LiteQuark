using System.Runtime.CompilerServices;

namespace LiteQuark.Runtime
{
    public enum EaseKind
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InBack,
        OutBack,
        InOutBack
    }

    public static class EaseUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sample(EaseKind kind, float t)
        {
            switch (kind)
            {
                case EaseKind.Linear:
                    return Linear(t);
                case EaseKind.InQuad:
                    return InQuad(t);
                case EaseKind.OutQuad:
                    return OutQuad(t);
                case EaseKind.InOutQuad:
                    return InOutQuad(t);
                case EaseKind.InCubic:
                    return InCubic(t);
                case EaseKind.OutCubic:
                    return OutCubic(t);
                case EaseKind.InOutCubic:
                    return InOutCubic(t);
                case EaseKind.InQuart:
                    return InQuart(t);
                case EaseKind.OutQuart:
                    return OutQuart(t);
                case EaseKind.InOutQuart:
                    return InOutQuart(t);
                case EaseKind.InQuint:
                    return InQuint(t);
                case EaseKind.OutQuint:
                    return OutQuint(t);
                case EaseKind.InOutQuint:
                    return InOutQuint(t);
                case EaseKind.InBack:
                    return InBack(t);
                case EaseKind.OutBack:
                    return OutBack(t);
                case EaseKind.InOutBack:
                    return InOutBack(t);
            }

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Linear(float t)
        {
            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InQuad(float t)
        {
            return t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutQuad(float t)
        {
            return t * (2 - t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutQuad(float t)
        {
            return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InCubic(float t)
        {
            return t * t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutCubic(float t)
        {
            return (--t) * t * t + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutCubic(float t)
        {
            return t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InQuart(float t)
        {
            return t * t * t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutQuart(float t)
        {
            return 1 - (--t) * t * t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutQuart(float t)
        {
            return t < 0.5f ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InQuint(float t)
        {
            return t * t * t * t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutQuint(float t)
        {
            return 1 + (--t) * t * t * t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutQuint(float t)
        {
            return t < 0.5f ? 16 * t * t * t * t * t : 1 + 16 * (--t) * t * t * t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InBack(float t)
        {
            const float s = 1.70158f; // 定义一个超调参数
            return t * t * ((s + 1) * t - s); // 根据三次方程的公式计算值
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutBack(float t)
        {
            const float s = 1.70158f; // 定义一个超调参数
            return (--t) * t * ((s + 1) * t + s) + 1; // 根据三次方程的公式计算值
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutBack(float t)
        {
            const float s = 1.70158f * 1.525f; // 定义一个超调参数
            return t < 0.5f ? (t * t * ((s + 1) * t - s)) * 0.5f : ((t - 1) * (t - 1) * ((s + 1) * (t - 1) + s) + 1) * 0.5f; // 根据三次方程的公式计算值
        }
    }
}