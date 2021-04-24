using System.Numerics;

namespace GpuCompute
{
    public struct RayHit
    {
        public Vector3 HitPosition;
        public Vector3 Normal;
        public Solid Solid;
        public float T;
        public Ray Ray;

        public static RayHit Create(Vector3 hitPosition, Vector3 normal, Solid solid, float t, Ray ray)
        {
            RayHit hit;
            hit.HitPosition = hitPosition;
            hit.Normal = normal;
            hit.Solid = solid;
            hit.T = t;
            hit.Ray = ray;

            return hit;
        }
    }
}