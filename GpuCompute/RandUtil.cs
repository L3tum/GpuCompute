using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GpuCompute
{
    public static class RandUtil
    {
        public static uint XorShift(uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 15;
            return state;
        }

        //
        // public static float RandomFloat(ref uint state)
        // {
        //     return XorShift(ref state) * (1f / 4294967296f);
        // }
        //
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        public static Vector3 RandomInUnitDisk(ref RandomHelper randomHelper)
        {
            Vector3 p;
            do
            {
                p = 2f * new Vector3(randomHelper.RandomFloat(), randomHelper.RandomFloat(), 0) - new Vector3(1, 1, 0);
            } while (Vector3.Dot(p, p) >= 1f);

            return p;
        }
        //
        // public static Vector3 RandomInUnitSphere(ref uint state)
        // {
        //     Vector3 ret;
        //     do
        //     {
        //         ret = 2f * new Vector3(RandomFloat(ref state), RandomFloat(ref state), RandomFloat(ref state)) -
        //               Vector3.One;
        //     } while (ret.LengthSquared() >= 1f);
        //
        //     return ret;
        // }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        public static Vector3 RandomInUnitSphere(ref RandomHelper randomHelper)
        {
            Vector3 ret;
            do
            {
                ret = 2f * new Vector3(randomHelper.RandomFloat(), randomHelper.RandomFloat(),
                          randomHelper.RandomFloat()) -
                      Vector3.One;
            } while (ret.LengthSquared() >= 1f);

            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static Vector3 RandomInUnitHemisphere(float r1, float r2)
        {
            var sinTheta = MathF.Sqrt(1 - r1 * r1);
            var phi = 2 * MathF.PI * r2;
            var x = sinTheta * MathF.Cos(phi);
            var z = sinTheta * MathF.Sin(phi);

            return new Vector3(x, r1, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static void CreateLocalCoordinateSystem(Vector3 hitNormal, out Vector3 nt, out Vector3 nb)
        {
            if (hitNormal.X > hitNormal.Y)
            {
                nt = new Vector3(hitNormal.Z, 0, -hitNormal.X) /
                     MathF.Sqrt(hitNormal.X * hitNormal.X + hitNormal.Z * hitNormal.Z);
            }
            else
            {
                nt = new Vector3(0, -hitNormal.Z, hitNormal.Y) /
                     MathF.Sqrt(hitNormal.Y * hitNormal.Y + hitNormal.Z * hitNormal.Z);
            }

            nb = Vector3.Cross(hitNormal, nt);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public static Vector3 ConvertLocalToWorld(Vector3 sample, Vector3 hitNormal, Vector3 nb, Vector3 nt)
        {
            return new(
                sample.X * nb.X + sample.Y * hitNormal.X + sample.Z * nt.X,
                sample.X * nb.Y + sample.Y * hitNormal.Y + sample.Z * nt.Y,
                sample.X * nb.Z + sample.Y * hitNormal.Z + sample.Z * nt.Z
            );
        }
    }
}