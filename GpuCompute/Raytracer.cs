using System.Numerics;
using ComputeSharp;

// ReSharper disable UseObjectOrCollectionInitializer

// ReSharper disable RedundantExplicitArrayCreation

// ReSharper disable PatternAlwaysOfType

// ReSharper disable SuggestVarOrType_Elsewhere

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace GpuCompute
{
    [AutoConstructor]
    public readonly partial struct Raytracer : IComputeShader
    {
        public readonly IReadWriteTexture2D<Float4> Target;
        public readonly ReadOnlyBuffer<Solid> Solids;
        public readonly ReadOnlyBuffer<Solid> Lights;
        public readonly int SolidsLength;
        public readonly int LightsLength;
        public readonly Float3 Position;
        public readonly float Yaw;
        public readonly float Pitch;
        public readonly Float3 EyePosition;
        public readonly Float4 BackgroundColor;
        private const float MINIMUM_ILLUMINATION = 0.3f;

        private Float3 RotateYawPitch(Float3 source)
        {
            // Convert to radians
            float yawRads = Hlsl.Radians(Yaw);
            float pitchRads = Hlsl.Radians(Pitch);

            // Rotate around X axis (pitch)
            float y = source.Y * Hlsl.Cos(pitchRads) - source.Z * Hlsl.Sin(pitchRads);
            float z = source.Y * Hlsl.Sin(pitchRads) + source.Z * Hlsl.Cos(pitchRads);

            // Rotate around Y axis (yaw)
            float x = source.X * Hlsl.Cos(yawRads) + z * Hlsl.Sin(yawRads);
            z = -source.X * Hlsl.Sin(yawRads) + z * Hlsl.Cos(yawRads);

            return new Float3(x, y, z);
        }

        private Ray GetRay(float u, float v)
        {
            Float3 rayDirection =
                Vector3.Normalize(RotateYawPitch(Vector3.Normalize(new Float3(u, v, 0) - EyePosition)));

            Ray ray = new Ray();
            ray.Origin = EyePosition + Position;
            ray.Direction = rayDirection;
            return ray;
        }

        private bool CalculateIntersectionSphere(Solid solid, Ray ray, out Float3 hit)
        {
            float t = Hlsl.Dot(solid.Position - ray.Origin, ray.Direction);
            Float3 p = ray.Origin + ray.Direction * t;
            float y = Hlsl.Length(solid.Position - p);

            if (y < solid.Radius)
            {
                float x = Hlsl.Sqrt(solid.RadiusSquared - y * y);
                float t1 = t - x;
                if (t1 > 0)
                {
                    hit = ray.Origin + ray.Direction * t1;
                    return true;
                }
            }

            hit = Float3.Zero;
            return false;
        }

        private bool CalculateIntersection(Solid solid, Ray ray, out Float3 hit)
        {
            switch (solid.SolidType)
            {
                default:
                {
                    return CalculateIntersectionSphere(solid, ray, out hit);
                }
            }
        }

        private Float3 GetNormalAt(Solid solid, Float3 point)
        {
            switch (solid.SolidType)
            {
                default:
                {
                    return Vector3.Normalize(point - solid.Position);
                }
            }
        }

        private bool CastRay(Ray ray, out RayHit rayHit)
        {
            rayHit = new RayHit();
            rayHit.HitPosition = Float3.Zero;
            rayHit.Normal = Float3.Zero;
            rayHit.Solid = new Solid();
            bool hit = false;

            for (int i = 0; i < SolidsLength; i++)
            {
                Solid solid = Solids[i];
                bool hasHit = CalculateIntersection(solid, ray, out Float3 hitPosition);

                if (hasHit)
                {
                    if (Hlsl.Length(Hlsl.Distance(rayHit.HitPosition, ray.Origin)) >
                        Hlsl.Length(Hlsl.Distance(hitPosition, ray.Origin)))
                    {
                        rayHit.HitPosition = hitPosition;
                        rayHit.Normal = GetNormalAt(solid, hitPosition);
                        rayHit.Solid = solid;
                        hit = true;
                    }
                }
            }

            return hit;
        }

        private float GetBrightnessFromLights(RayHit hit)
        {
            float brightness = 0.0f;

            for (int i = 0; i < LightsLength; i++)
            {
                Solid light = Lights[i];
                Ray ray = new Ray();
                ray.Origin = light.Position;
                ray.Direction = Vector3.Normalize(hit.HitPosition - light.Position);
                bool hasHit = CastRay(ray, out RayHit lightHit);

                if (hasHit && lightHit.Solid.SolidId == hit.Solid.SolidId)
                {
                    brightness += Hlsl.Dot(hit.Normal, light.Position - hit.HitPosition);
                }
            }

            return Hlsl.Max(MINIMUM_ILLUMINATION, brightness);
        }

        private float GetSpecularBrightness(RayHit hit)
        {
            float specularFactor = 0.0f;
            Float3 cameraDirection = Vector3.Normalize(Position - hit.HitPosition);

            for (int i = 0; i < LightsLength; i++)
            {
                Solid light = Lights[i];
                Float3 lightDirection = Vector3.Normalize(hit.HitPosition - light.Position);
                Float3 lightReflectionVector =
                    lightDirection - hit.Normal * (2.0f * Hlsl.Dot(lightDirection, hit.Normal));
                specularFactor += Hlsl.Max(0, Hlsl.Min(1, Hlsl.Dot(lightReflectionVector, cameraDirection)));
            }

            return Hlsl.Pow(specularFactor, 2) * hit.Solid.Reflectivity;
        }

        private Float4 ComputePixelInfo(float u, float v)
        {
            Ray ray = GetRay(u, v);
            bool hasHit = CastRay(ray, out RayHit hit);

            if (hasHit)
            {
                float emission = GetBrightnessFromLights(hit);
                Float4 color = hit.Solid.Color;
                color.RGB *= emission;
                color.RGB += GetSpecularBrightness(hit);
                return color;
            }

            return BackgroundColor;
        }

        private Float2 GetNormalizedScreenCoordinates(int x, int y, int width, int height)
        {
            float u = 0, v = 0;
            if (width > height)
            {
                u = (float) (x - width / 2 + height / 2) / height * 2 - 1;
                v = -((float) y / height * 2 - 1);
            }
            else
            {
                u = (float) x / width * 2 - 1;
                v = -((float) (y - height / 2 + width / 2) / width * 2 - 1);
            }

            return new Float2(u, v);
        }

        public void Execute()
        {
            Float2 screenUv = GetNormalizedScreenCoordinates(ThreadIds.X, ThreadIds.Y, Target.Width, Target.Height);

            Target[ThreadIds.XY] = ComputePixelInfo(screenUv.X, screenUv.Y);
        }
    }
}