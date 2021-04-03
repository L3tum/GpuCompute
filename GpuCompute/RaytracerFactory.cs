using System;
using System.Linq;
using ComputeSharp;
using TerraFX.Interop;

// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace GpuCompute
{
    internal class RaytracerFactory : SwapChainApplication<Raytracer>
    {
        private static readonly Solid[] Solids =
        {
            // RGBA
            new Solid((int) SolidType.SPHERE, 0.4f, new Float3(-1, 0, 0), new Float4(1f, 0f, 0f, 1f), 0.0f, 0f, 0),
            new Solid((int) SolidType.SPHERE, 0.4f, Float3.Zero, new Float4(0f, 1f, 0f, 1f), 0.0f, 0f, 1),
            new Solid((int) SolidType.SPHERE, 0.4f, Float3.UnitX, new Float4(0f, 0f, 1f, 1f), 0.0f, 0f, 2),
            new Solid((int) SolidType.SPHERE, 0.4f, new Float3(0, 2, -1), new Float4(1f, 1f, 1f, 1f), 0.0f, 1.0f, 3)
        };

        private static ReadOnlyBuffer<Solid>? solidsBuffer;
        private static ReadOnlyBuffer<Solid>? lightsBuffer;

        public Raytracer CreateRaytracer(IReadWriteTexture2D<Float4> texture, TimeSpan time)
        {
            const float fov = 30;
            var eyePosition = new Float3(0, 0, -1 / MathF.Tan(ConvertToRadians(fov / 2f)));
            return new Raytracer(texture, solidsBuffer!, lightsBuffer!, Solids.Length, lightsBuffer!.Length,
                Float3.Zero, 0, 0, eyePosition,
                new Float4(0, 0, 0, 1f));
        }

        protected override Raytracer GetComputeShader(IReadWriteTexture2D<Float4> tex, TimeSpan time)
        {
            return CreateRaytracer(tex, time);
        }

        public override void OnInitialize(HWND hwnd)
        {
            base.OnInitialize(hwnd);
            solidsBuffer = Gpu.Default.AllocateReadOnlyBuffer(Solids);
            lightsBuffer =
                Gpu.Default.AllocateReadOnlyBuffer(Solids.Where(solid => solid.Emission > 0.0f).ToArray());
        }

        private float ConvertToRadians(float angle)
        {
            return MathF.PI / 180.0f * angle;
        }
    }
}