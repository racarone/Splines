using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

namespace Splines
{
    public static class CurveMath
    {
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InterpolatePosition(float p0, float m0, float m1, float p1, float t)
        {
            // Unrolled the equations to avoid precision issue.
            // (2 * t^3 -3 * t^2 +1) * p0 + (t^3 - 2 * t^2 + t) * m0 + (-2 * t^3 + 3 * t^2) * p1 + (t^3 - t^2) * m1

            float a = 2.0f * p0 + m0 - 2.0f * p1 + m1;
            float b = -3.0f * p0 - 2.0f * m0 + 3.0f * p1 - m1;
            float c = m0;
            float d = p0;

            return t * (t * (a * t + b) + c) + d;
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InterpolatePosition(float3 p0, float3 m0, float3 m1, float3 p1, float t)
        {
            // Unrolled the equations to avoid precision issue.
            // (2 * t^3 -3 * t^2 +1) * p0 + (t^3 - 2 * t^2 + t) * m0 + (-2 * t^3 + 3 * t^2) * p1 + (t^3 - t^2) * m1

            float3 a = 2.0f * p0 + m0 - 2.0f * p1 + m1;
            float3 b = -3.0f * p0 - 2.0f * m0 + 3.0f * p1 - m1;
            float3 c = m0;
            float3 d = p0;

            return t * (t * (a * t + b) + c) + d;
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InterpolateTangent(float p0, float m0, float m1, float p1, float t)
        {
            float a =  6f*p0 + 3f*m0 + 3f*m1 - 6f*p1;
            float b = -6f*p0 - 4f*m0 - 2f*m1 + 6f*p1;
            float c = m0;

            float t2 = t  * t;
            return ((a * t2) + (b * t) + c);
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InterpolateTangent(float3 p0, float3 m0, float3 m1, float3 p1, float t)
        {
            float3 a =  6f*p0 + 3f*m0 + 3f*m1 - 6f*p1;
            float3 b = -6f*p0 - 4f*m0 - 2f*m1 + 6f*p1;
            float3 c = m0;

            float t2 = t  * t;
            return ((a * t2) + (b * t) + c);
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InterpolateDirection(float3 p0, float3 m0, float3 m1, float3 p1, float t)
        {
            return normalizesafe(InterpolateTangent(p0, m0, m1, p1, t));
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InterpolateNormal(float p0, float m0, float m1, float p1, float t)
        {
            return (12f*p0 + 6f*m0 + 6f*m1 - 12f*p1 * t) + (-6f*p0 - 4f*m0 - 2f*m1 + 6f*p1);
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 InterpolateNormal(float3 p0, float3 m0, float3 m1, float3 p1, float t)
        {
            return (12f*p0 + 6f*m0 + 6f*m1 - 12f*p1 * t) + (-6f*p0 - 4f*m0 - 2f*m1 + 6f*p1);
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion InterpolateRotation(quaternion p0, quaternion m0, quaternion m1, quaternion p1, float t)
        {
            // Use the short path from q1 to q2 to prevent axis flipping.
            quaternion q1 = slerp(p0, p1, t);
            quaternion q2 = SlerpFullPath(m0, m1, t);
            return normalizesafe(SlerpFullPath(q1, q2, 2.0f * t * (1.0f - t)));
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion SlerpFullPath(quaternion q1, quaternion q2, float t)
        {
            float dt = clamp(dot(q1, q2), -1.0f, 1.0f);
            float angle = acos(dt);

            if (abs(angle) < 1e-4f)
                return q1;

            float w1 = sin(angle);
            float w2 = 1.0f / w1;

            float s1 = sin((1.0f - t) * angle) * w2;
            float s2 = sin(t * angle) * w2;

            return q1.value * s1 + q2.value * s2;
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ComputeOrientation(float3 p0, float3 m0, float3 m1, float3 p1, float t)
        {
            return ComputeOrientation(p0, m0, m1, p1, t, new float3(0f, 1f, 0f));
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ComputeOrientation(float3 p0, float3 m0, float3 m1, float3 p1, float t, float3 upVector)
        {
            return quaternion.LookRotationSafe(InterpolateDirection(p0, m0, m1, p1, t), upVector);
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ComputeTangent(float prevPoint, float thisPoint, float nextPoint, float tension)
        {
            return (1f - tension) * ((thisPoint - prevPoint) + (nextPoint - thisPoint));
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ComputeTangent(float3 prevPoint, float3 thisPoint, float3 nextPoint, float tension)
        {
            return (1f - tension) * ((thisPoint - prevPoint) + (nextPoint - thisPoint));
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ComputeTangent(quaternion prevPoint, quaternion thisPoint, quaternion nextPoint, float tension)
        {
            quaternion invP = inverse(thisPoint);
            quaternion part1 = log(mul(invP, prevPoint));
            quaternion part2 = log(mul(invP, nextPoint));
            quaternion preExp = (part1.value + part2.value) * -0.5f;
            return mul(thisPoint, exp(preExp));
        }

        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bounds ComputeBounds(float3 p0, float3 m0, float3 m1, float3 p1)
        {
            Bounds bounds = new Bounds();
            ComputeBounds(p0.x, m0.x, m1.x, p1.x, out float minX, out float maxX);
            ComputeBounds(p0.y, m0.y, m1.y, p1.y, out float minY, out float maxY);
            ComputeBounds(p0.z, m0.z, m1.z, p1.z, out float minZ, out float maxZ);
            bounds.SetMinMax(new float3(minX, minY, minZ), new float3(maxX, maxY, maxZ));
            return bounds;
        }

        // Calculates the potential bounds of a single cubic bezier curve
        // https://stackoverflow.com/questions/2587751/an-algorithm-to-find-bounding-box-of-closed-bezier-curves
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ComputeBounds(float p0, float m0, float m1, float p1, out float outMin, out float outMax)
        {
            outMin = min(p0, p1);
            outMax = max(p0, p1);

            float a =  6f*p0 + 3f*m0 + 3f*m1 - 6f*p1;
            float b = -6f*p0 - 4f*m0 - 2f*m1 + 6f*p1;
            float c = m0;

            float b2Ac = b * b - 4f * a * c;
            if (!(b2Ac > 0.0f) || !(abs(a) > float.Epsilon))
                return;

            float sqrt_b2Ac = sqrt(b2Ac);

            float t1 = (-b + sqrt_b2Ac) / (2f * a);
            if (t1 >= 0f && t1 <= 1f)
            {

                float value = InterpolatePosition(p0, m0, m1, p1, t1);
                outMin = min(outMin, value);
                outMax = max(outMax, value);
            }

            var t2 = (-b - sqrt_b2Ac) / (2f * a);
            if (t2 >= 0f && t2 <= 1f)
            {
                float value = InterpolatePosition(p0, m0, m1, p1, t2);
                outMin = min(outMin, value);
                outMax = max(outMax, value);
            }
        }

        struct GaussLengendreCoefficient
        {
            public float abscissa;
            public float weight;
        }

        static readonly GaussLengendreCoefficient[] s_GaussLengendreCoefficient =
        {
            new GaussLengendreCoefficient {abscissa =  0.0f,        weight = 0.5688889f },
            new GaussLengendreCoefficient {abscissa = -0.5384693f,  weight = 0.47862867f},
            new GaussLengendreCoefficient {abscissa =  0.5384693f,  weight = 0.47862867f},
            new GaussLengendreCoefficient {abscissa = -0.90617985f, weight = 0.23692688f},
            new GaussLengendreCoefficient {abscissa =  0.90617985f, weight = 0.23692688f}
        };

        // Compute a length of a spline segment by using 5-point Legendre-Gauss quadrature
        // https://medium.com/@all2one/how-to-compute-the-length-of-a-spline-e44f5f04c40
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ComputeArcLength(float3 p0, float3 m0, float3 m1, float3 p1, float t, float3 scale)
        {
            // Cubic Hermite spline derivative coeffcients
            float3 c0 = m0;
            float3 c1 = 6f*(p1 - p0) - 4f*m0 - 2f*m1;
            float3 c2 = 6f*(p0 - p1) + 3f*(m0 + m1);

            float accumulatedLength = 0f;

            for (int i = 0; i < 5; ++i)
            {
                // This and the final (0.5 *) below are needed for a change of interval to [0, 1] from [-1, 1]
                float t2 = (t * 0.5f) * (1f + s_GaussLengendreCoefficient[i].abscissa);
                float3 derivative = c0 + t2 * (c1 + t2 * c2);
                accumulatedLength += length(derivative * scale) * s_GaussLengendreCoefficient[i].weight;
            }

            return accumulatedLength * (t * 0.5f);
        }

        // Compute a length of a spline segment by using 5-point Legendre-Gauss quadrature
        // https://medium.com/@all2one/how-to-compute-the-length-of-a-spline-e44f5f04c40
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ComputeArcLength(float3 p0, float3 m0, float3 m1, float3 p1, float t)
        {
            return ComputeArcLength(p0, m0, m1, p1, t, Vector3.one);
        }
    }
}
