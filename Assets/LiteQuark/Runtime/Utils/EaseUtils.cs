using System.Runtime.CompilerServices;
using UnityEngine;

namespace LiteQuark.Runtime
{
    public enum EaseKind
    {
        Linear,
        // Sine
        InSine,
        OutSine,
        InOutSine,
        // Quad
        InQuad,
        OutQuad,
        InOutQuad,
        // Cubic
        InCubic,
        OutCubic,
        InOutCubic,
        // Quart
        InQuart,
        OutQuart,
        InOutQuart,
        // Quint
        InQuint,
        OutQuint,
        InOutQuint,
        // Expo
        InExpo,
        OutExpo,
        InOutExpo,
        // Circ
        InCirc,
        OutCirc,
        InOutCirc,
        // Back
        InBack,
        OutBack,
        InOutBack,
        // Elastic
        InElastic,
        OutElastic,
        InOutElastic,
        // Bounce
        InBounce,
        OutBounce,
        InOutBounce,
    }

    public static class EaseUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sample(EaseKind kind, float t)
        {
            switch (kind)
            {
                case EaseKind.Linear: return Linear(t);
                // Sine
                case EaseKind.InSine: return InSine(t);
                case EaseKind.OutSine: return OutSine(t);
                case EaseKind.InOutSine: return InOutSine(t);
                // Quad
                case EaseKind.InQuad: return InQuad(t);
                case EaseKind.OutQuad: return OutQuad(t);
                case EaseKind.InOutQuad: return InOutQuad(t);
                // Cubic
                case EaseKind.InCubic: return InCubic(t);
                case EaseKind.OutCubic: return OutCubic(t);
                case EaseKind.InOutCubic: return InOutCubic(t);
                // Quart
                case EaseKind.InQuart: return InQuart(t);
                case EaseKind.OutQuart: return OutQuart(t);
                case EaseKind.InOutQuart: return InOutQuart(t);
                // Quint
                case EaseKind.InQuint: return InQuint(t);
                case EaseKind.OutQuint: return OutQuint(t);
                case EaseKind.InOutQuint: return InOutQuint(t);
                // Expo
                case EaseKind.InExpo: return InExpo(t);
                case EaseKind.OutExpo: return OutExpo(t);
                case EaseKind.InOutExpo: return InOutExpo(t);
                // Circ
                case EaseKind.InCirc: return InCirc(t);
                case EaseKind.OutCirc: return OutCirc(t);
                case EaseKind.InOutCirc: return InOutCirc(t);
                // Back
                case EaseKind.InBack: return InBack(t);
                case EaseKind.OutBack: return OutBack(t);
                case EaseKind.InOutBack: return InOutBack(t);
                // Elastic
                case EaseKind.InElastic: return InElastic(t);
                case EaseKind.OutElastic: return OutElastic(t);
                case EaseKind.InOutElastic: return InOutElastic(t);
                // Bounce
                case EaseKind.InBounce: return InBounce(t);
                case EaseKind.OutBounce: return OutBounce(t);
                case EaseKind.InOutBounce: return InOutBounce(t);
            }

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Linear(float t)
        {
            return t;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InSine(float t)
        {
            return 1f - Mathf.Cos((t * Mathf.PI) / 2f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutSine(float t)
        {
            return Mathf.Sin((t * Mathf.PI) / 2f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutSine(float t)
        {
            return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InQuad(float t)
        {
            return t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InCubic(float t)
        {
            return t * t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InQuart(float t)
        {
            return t * t * t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutQuart(float t)
        {
            return 1f - Mathf.Pow(1f - t, 4f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutQuart(float t)
        {
            return t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InQuint(float t)
        {
            return t * t * t * t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutQuint(float t)
        {
            return 1f - Mathf.Pow(1f - t, 5f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutQuint(float t)
        {
            return t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InExpo(float t)
        {
            return t == 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutExpo(float t)
        {
            return Mathf.Approximately(t, 1f) ? 1f : 1f - Mathf.Pow(2f, -10f * t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutExpo(float t)
        {
            return t == 0f ? 0f : Mathf.Approximately(t, 1f) ? 1 : t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) / 2f : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InCirc(float t)
        {
            return 1f - Mathf.Sqrt(1f - Mathf.Pow(t, 2f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutCirc(float t)
        {
            return Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutCirc(float t)
        {
            return t < 0.5f
                ? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) / 2f
                : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) / 2f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;
            return t < 0.5f
                ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
                : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;
            
            return t == 0f ? 0f : Mathf.Approximately(t, 1f) ? 1f : -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * c4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutElastic(float t)
        {
            const float c4 = (2f * Mathf.PI) / 3f;

            return t == 0f ? 0f : Mathf.Approximately(t, 1f) ? 1f : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutElastic(float t)
        {
            const float c5 = (2f * Mathf.PI) / 4.5f;

            return t == 0f
                ? 0f
                : Mathf.Approximately(t, 1f)
                    ? 1f
                    : t < 0.5f
                        ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f
                        : (Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f + 1f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InBounce(float t)
        {
            return 1f - OutBounce(1f - t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1f / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2f / d1)
            {
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            }
            else if (t < 2.5f / d1)
            {
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            }
            else
            {
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float InOutBounce(float t)
        {
            return t < 0.5f
                ? (1f - OutBounce(1f - 2f * t)) / 2f
                : (1f + OutBounce(2f * t - 1f)) / 2f;
        }
    }
}