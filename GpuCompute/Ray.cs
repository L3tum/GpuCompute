using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GpuCompute
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Ray
    {
        public Vector3 Origin;
        public Vector3 Direction;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static Ray Create(Vector3 origin, Vector3 direction)
        {
            Ray r;
            r.Origin = origin;
            r.Direction = direction;
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static Vector3 PointAt(Ray ray, float t)
        {
            return ray.Origin + ray.Direction * t;
        }
    }
}