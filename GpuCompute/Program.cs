using GpuCompute.Win32;

namespace GpuCompute
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Win32Application tracer = new RaytracerFactory();
            Win32ApplicationRunner.Run(tracer);

            // if (File.Exists("output.png"))
            // {
            //     File.Delete("output.png");
            // }
            //
            // // DESTINATION
            // using var bitmap = new Bitmap(1920, 1080, PixelFormat.Format32bppRgb);
            // var bitmapData = bitmap.LockBits(new Rectangle(0, 0, 1920, 1080), ImageLockMode.ReadWrite,
            //     PixelFormat.Format32bppRgb);
            // Span<Bgra32> bitmapSpan;
            // unsafe
            // {
            //     bitmapSpan = new Span<Bgra32>((Bgra32*) bitmapData.Scan0, bitmapData.Width * bitmapData.Height);
            // }
            //
            // using var destination =
            //     Gpu.Default.AllocateReadWriteTexture2D<Bgra32, Float4>(bitmapSpan, bitmap.Width, bitmap.Height);
            //
            // var solids = new[]
            // {
            //     new Solid((int) SolidType.SPHERE, 0.4f, -Float3.UnitX, new Float4(1f, 1f, 1f, 1f), 0),
            //     new Solid((int) SolidType.SPHERE, 0.4f, Float3.Zero, new Float4(1f, 1f, 1f, 1f), 1),
            //     new Solid((int) SolidType.SPHERE, 0.4f, Float3.UnitX, new Float4(1f, 1f, 1f, 1f), 2)
            // };
            // using var solidsBuffer = Gpu.Default.AllocateConstantBuffer(solids);
            // var raytracer = new Raytracer(destination, solidsBuffer, solids.Length, Float3.Zero, 0, 0, 60,
            //     new Float4(0, 0, 0, 1f), 1920, 1080);
            // ReflectionServices.GetShaderInfo<Raytracer>(out var shaderInfo);
            // Console.WriteLine(shaderInfo.HlslSource);
            // Gpu.Default.For(1920, 1080, raytracer);
            //
            // destination.CopyTo(bitmapSpan);
            // bitmap.UnlockBits(bitmapData);
            //
            // bitmap.Save("output.png");
            //
            // Console.WriteLine("DONE");
        }
    }
}