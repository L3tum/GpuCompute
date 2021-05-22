using System.Numerics;
using ShaderGen;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable SuggestVarOrType_BuiltInTypes

[assembly: ComputeShaderSet("Raytracer", "GpuCompute.Shaders.RaytracerCompute.CS")]

namespace GpuCompute.Shaders
{
    public class RaytracerCompute
    {
        public float RandomFloat(uint seed)
        {
            return seed * (1f / 4294967296f);
        }

        public float RandomFloatBetween(uint seed, float max, float min)
        {
            return ShaderBuiltins.Mod(RandomFloat(seed), max - min) + min;
        }

        public uint XorShift(uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 15;
            return state;
        }

        public RayHit TestRay(Ray ray)
        {
            RayHit hit;
            hit.Normal = Vector3.Zero;
            hit.Ray = ray;
            hit.T = 0;
            hit.HitPosition = Vector3.Zero;
            hit.Solid = Solids[0];
            float closest = 9999999f;
            for (uint i = 0; i < Params.SphereCount; i++)
            {
                // if (Solid.Hit(Solids[i], ray, 0.001f, closest, out RayHit tempHit))
                // {
                //     hit = tempHit;
                //     closest = hit.T;
                // }
            }

            return hit;
        }

        // Cannot call static version in RayTracingApplication -- it's not possible to pass StructuredBuffers as parameters in GLSL.
        public Vector4 Color(Ray ray)
        {
            Vector4 color = Vector4.Zero;
            Vector4 currentAttenuation = Vector4.One; // Start at full strength

            for (int curDepth = 0; curDepth < 1; curDepth++)
            {
                RayHit hit = TestRay(ray);
                
                if(hit.T > 0)
                {
                    color = color + hit.Solid.Color;
                }
                else // Hit nothing -- sky
                {
                    Vector3 unitDir = Vector3.Normalize(ray.Direction);
                    float t = 0.5f * (unitDir.Y + 1f);
                    Vector4 backgroundColor;
                    backgroundColor.X = 0.5f;
                    backgroundColor.Y = 0.7f;
                    backgroundColor.Z = 1f;
                    backgroundColor.W = 1f;
                    color += currentAttenuation * ((1f - t) * Vector4.One + t * backgroundColor);
                }
            }

            return color;
        }

        [ComputeShader(16, 16, 1)]
        public void CS()
        {
            UInt3 dtId = ShaderBuiltins.DispatchThreadID;
            Vector4 color = Vector4.Zero;
            uint randState = (dtId.X * 1973 + dtId.Y * 9277 + Params.FrameCount * 26699) | 1;
            Ray ray = Camera.GetRay(Params.Camera, dtId.X, dtId.Y);
            color += Color(ray);
            ShaderBuiltins.Store(Output, new UInt2(dtId.X, dtId.Y), color);
        }
#nullable disable
        public StructuredBuffer<Solid> Solids;
        public RWTexture2DResource<Vector4> Output;
        public SceneParams Params;
        public AtomicBufferUInt32 RayCount;

#nullable restore
    }
}