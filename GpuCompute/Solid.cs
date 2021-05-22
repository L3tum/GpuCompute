using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GpuCompute
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Solid
    {
        public readonly SolidType SolidType;
        public readonly float Radius;
        public readonly Vector3 Position;
        public readonly float RadiusSquared;
        public readonly Vector4 Color;
        public readonly float Reflectivity;
        public readonly float Emission;
        public readonly int SolidId;
        public readonly float Roughness;

        public Solid(SolidType solidType, float radius, Vector3 position, Vector4 color, float reflectivity,
            float emission, float roughness, int solidId)
        {
            SolidType = solidType;
            Radius = radius;
            RadiusSquared = radius * radius;
            Position = position;
            Color = color;
            Reflectivity = reflectivity;
            Emission = emission;
            Roughness = roughness;
            SolidId = solidId;
        }

        public static Solid CreateSphere(float radius, Vector3 position, Vector3 color, float reflectivity,
            float emission, float roughness, int solidId)
        {
            return new(SolidType.SPHERE, radius, position, new Vector4(color, 1.0f), reflectivity, emission, roughness,
                solidId);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        private static bool HitSphere(Solid solid, Ray ray, float tMin, float tMax, out RayHit hit)
        {
            Unsafe.SkipInit(out hit);
            hit.Ray = ray;
            var oc = ray.Origin - solid.Position;
            var a = Vector3.Dot(ray.Direction, ray.Direction);
            var b = Vector3.Dot(oc, ray.Direction);
            var c = Vector3.Dot(oc, oc) - solid.RadiusSquared;
            var discriminant = b * b - a * c;
            if (discriminant > 0)
            {
                var tmp = MathF.Sqrt(b * b - a * c);
                var t = (-b - tmp) / a;
                if (t < tMax && t > tMin)
                {
                    var position = Ray.PointAt(ray, t);
                    var normal = (position - solid.Position) / solid.Radius;
                    hit.HitPosition = position;
                    hit.Normal = normal;
                    hit.Solid = solid;
                    hit.T = t;
                    return true;
                }

                t = (-b + tmp) / a;
                if (t < tMax && t > tMin)
                {
                    var position = Ray.PointAt(ray, t);
                    var normal = (position - solid.Position) / solid.Radius;
                    hit.HitPosition = position;
                    hit.Normal = normal;
                    hit.Solid = solid;
                    hit.T = t;
                    return true;
                }
            }
            
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        public static bool Hit(Solid solid, Ray ray, float tMin, float tMax, out RayHit hit)
        {
            return solid.SolidType switch
            {
                SolidType.SPHERE => HitSphere(solid, ray, tMin, tMax, out hit),
                _ => throw new NotSupportedException()
            };
        }
    }


    public enum SolidType
    {
        SPHERE = 0,
        PLANE = 1,
        BOX = 2
    }
}