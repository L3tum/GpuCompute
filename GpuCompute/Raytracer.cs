using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ImGuiNET;
using Veldrid;

namespace GpuCompute
{
    public static class Raytracer
    {
        internal const int BlockWidth = 150;
        internal const int BlockHeight = 150;
        internal static bool SpecularLightSampling = Constants.DEFAULT_SPECULAR_SAMPLING;
        internal static int SkipPixelProbability = Constants.DEFAULT_SKIP_PROBABILITY;
        internal static Solid[] Solids = null!;
        internal static ulong RaysPerFrame;
        internal static Solid[] Lights = null!;
        internal static RaytracerSettings Settings = RaytracerSettings.Create();
        internal static RgbaFloat[] FrameBuffer = null!;
        internal static SceneParams SceneParams;
        internal static int Width;
        internal static int Height;
        internal static float InvertedWidth;
        internal static float InvertedHeight;
        internal static int Rows;
        internal static int Columns;
        private static Solid emptySolid = new Solid();
        private static ThreadLocal<RandomHelper>? RandomHelper;
        private static Vector4 minimumIlluminationNoAlpha;
        private static Vector4 minimumIlluminationFullAlpha;

        internal static void LoadScene(Scene scene)
        {
            SceneParams.Camera = scene.Camera;
            SceneParams.SphereCount = (uint) scene.Solids.Count;
            SceneParams.BackgroundColor = scene.BackgroundColor;
            Solids = scene.Solids.ToArray();
            Lights = scene.Solids.Where(solid => solid.Emission > 0.0f).ToArray();
            ThreadPool.SetMaxThreads(Environment.ProcessorCount, 6);
            ThreadPool.SetMinThreads(Environment.ProcessorCount / 2, 0);
        }

        internal static void RenderSettings()
        {
            if (ImGui.Begin("Settings"))
            {
                if (ImGui.BeginMenu("Raytracer Settings"))
                {
                    ImGui.Checkbox("Diffuse Light Sampling", ref Settings.DiffuseLightSampling);
                    ImGui.Checkbox("Sample One Light", ref Settings.DiffuseLightSamplingOneLight);
                    ImGui.DragInt("Sample Light", ref Settings.DiffuseLightSamplingLight, 1, 0, Lights.Length - 1);
                    ImGui.Checkbox("Specular Sampling", ref SpecularLightSampling);
                    ImGui.Checkbox("Randomize Bouncing", ref Settings.RandomizeBounceOnRoughness);
                    ImGui.Checkbox("Randomize Camera Ray", ref Settings.RandomizeCameraRays);
                    ImGui.DragInt("Skip Pixel Probability", ref SkipPixelProbability, 1, 0, 50);
                    ImGui.DragInt("Number of Bounces", ref Settings.MaxBounces, 1, 0, 50);
                    ImGui.DragInt("Number of Samples", ref Settings.NumberOfSamples, 16, 16, 81920);
                    ImGui.DragFloat("Minimum Illumination", ref Settings.MinimumIllumination, 0.1f, 0f, 1f);
                }
            }

            minimumIlluminationNoAlpha = new Vector4(Settings.MinimumIllumination, Settings.MinimumIllumination,
                Settings.MinimumIllumination, 0.0f);
            minimumIlluminationFullAlpha = new Vector4(Settings.MinimumIllumination, Settings.MinimumIllumination,
                Settings.MinimumIllumination, 1.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        private static bool CastRay(Ray ray, out RayHit rayHit, ref uint rayCount)
        {
            rayCount++;
            rayHit.HitPosition = Vector3.Zero;
            rayHit.Normal = Vector3.Zero;
            rayHit.Solid = emptySolid;
            rayHit.T = 0;
            rayHit.Ray = ray;
            var hit = false;
            var t = float.MaxValue;

            foreach (var solid in Solids)
            {
                if (Solid.Hit(solid, ray, float.Epsilon, t, out var solidHit))
                {
                    rayHit = solidHit;
                    t = solidHit.T;
                    hit = true;
                }
            }

            return hit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static Vector4 SampleLight(RayHit hit, Solid light, ref uint rayCount)
        {
            Ray ray;
            ray.Origin = light.Position;
            ray.Direction = Vector3.Normalize(light.Position - hit.HitPosition);
            var hasHit = CastRay(ray, out var lightHit, ref rayCount);

            if (hasHit && lightHit.Solid.SolidId == light.SolidId)
            {
                // return light.Color * (Vector3.Dot(hit.Normal, light.Position - hit.HitPosition) * light.Emission);
                return light.Color * light.Emission * Vector3.Dot(ray.Direction, hit.Normal);
                // return light.Color * light.Emission / (4 * MathF.PI * (hit.HitPosition - light.Position).Length());
            }

            return Vector4.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        private static Vector4 GetBrightnessFromLights(RayHit hit, ref uint rayCount)
        {
            var brightness = Vector4.Zero;

            if (Settings.DiffuseLightSamplingOneLight)
            {
                if (Settings.DiffuseLightSamplingLight < Lights.Length)
                {
                    var light = Lights[Settings.DiffuseLightSamplingLight];
                    brightness += SampleLight(hit, light, ref rayCount);
                }
            }
            else
            {
                foreach (var light in Lights)
                {
                    brightness += SampleLight(hit, light, ref rayCount);
                }
            }

            return Vector4.Max(brightness, minimumIlluminationNoAlpha);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        private static float GetSpecularBrightness(RayHit hit)
        {
            var specularFactor = 0.0f;
            var cameraDirection = Vector3.Normalize(SceneParams.Camera.Origin - hit.HitPosition);

            foreach (var light in Lights)
            {
                var lightDirection = Vector3.Normalize(hit.HitPosition - light.Position);
                var lightReflectionVector =
                    lightDirection - hit.Normal * (2.0f * Vector3.Dot(lightDirection, hit.Normal));
                specularFactor += MathF.Max(0, MathF.Min(1, Vector3.Dot(lightReflectionVector, cameraDirection)));
            }

            return MathF.Pow(specularFactor, 2) * hit.Solid.Reflectivity;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        private static Vector4 GetColorFromBounce(RayHit hit, ref uint rayCount, ref RandomHelper randomHelper,
            int rayDepth = 0)
        {
            rayDepth++;
            if (rayDepth > Settings.MaxBounces)
            {
                return Vector4.Zero;
            }

            Ray ray;
            ray.Origin = hit.HitPosition;
            // ray.Direction = hit.HitPosition + hit.Normal +
            //                 (Settings.RandomizeBounceOnRoughness
            //                     ? RandUtil.RandomInUnitSphere(ref randomHelper) * hit.Solid.Roughness
            //                     : Vector3.Zero) -
            //                 hit.HitPosition;
            ray.Direction = Vector3.Reflect(hit.Ray.Direction, hit.Normal + (Settings.RandomizeBounceOnRoughness
                ? RandUtil.RandomInUnitSphere(ref randomHelper) * hit.Solid.Roughness
                : Vector3.Zero));
            return GetColorForRay(ray, ref rayCount, ref randomHelper, rayDepth);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        private static Vector4 GetColorForRay(Ray ray, ref uint rayCount, ref RandomHelper randomHelper,
            int rayDepth = 0)
        {
            var color = Vector4.Zero;

            if (rayDepth <= Settings.MaxBounces && CastRay(ray, out var hit, ref rayCount))
            {
                // Explicit Light sampling
                var brightness = Settings.DiffuseLightSampling
                    ? GetBrightnessFromLights(hit, ref rayCount)
                    : minimumIlluminationFullAlpha;

                // Bounce the ray
                var bounceColor = GetColorFromBounce(hit, ref rayCount, ref randomHelper, rayDepth);

                // Specular brightness
                var specularBrightness = rayDepth == 0 && SpecularLightSampling ? GetSpecularBrightness(hit) : 0.0f;

                // Add the samples
                var hitColor = hit.Solid.Color * (brightness / MathF.PI);
                hitColor += bounceColor;
                hitColor += new Vector4(specularBrightness, specularBrightness, specularBrightness, 0f);
                // The emission by the light itself (if it is one)
                hitColor += hit.Solid.Color * hit.Solid.Emission;

                color += hitColor;

                if (rayDepth > 0)
                {
                    color *= 1f - hit.Solid.Roughness;
                }
            }
            else
            {
                color += SceneParams.BackgroundColor;
            }

            return color;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void RenderCpuNew()
        {
            var frameRays = 0u;
            var numberOfBlocks = Rows * Columns;

            using var countdownEvent = new CountdownEvent(numberOfBlocks);

            for (var y = 0; y < Rows; y++)
            {
                var startHeight = y * BlockHeight;
                var endHeight = (y + 1) * BlockHeight;
                endHeight = endHeight > Height ? Height : endHeight;

                for (var x = 0; x < Columns; x++)
                {
                    var startWidth = x * BlockWidth;
                    var endWidth = (x + 1) * BlockWidth;
                    endWidth = endWidth > Width ? Width : endWidth;

                    ThreadPool.QueueUserWorkItem(data =>
                    {
                        var valueData = (ValueTuple<int, int, int, int>) data!;
                        var (x1, x2, y1, y2) = valueData;
                        var rayCount = 0u;
                        RandomHelper ??=
                            new ThreadLocal<RandomHelper>(() => new RandomHelper((uint) new Random().Next()));
                        var randomHelper = RandomHelper.Value;

                        for (var ya = (uint) y1; ya < y2; ya++)
                        {
                            var v = ya * InvertedHeight;

                            for (var xa = (uint) x1; xa < x2; xa++)
                            {
                                randomHelper.Seed(xa ^ ya);

                                if (SkipPixelProbability > 0 &&
                                    randomHelper.GetRandomBetween(100, 0) < SkipPixelProbability)
                                {
                                    continue;
                                }

                                var u = xa * InvertedWidth;
                                var ray = Camera.GetRay(SceneParams.Camera, u, v);
                                var color = Vector4.Zero;
                                for (var i = 0; i < Settings.NumberOfSamples; i++)
                                {
                                    color += GetColorForRay(ray, ref rayCount, ref randomHelper);
                                }

                                FrameBuffer[ya * Width + xa] = new RgbaFloat(color / Settings.NumberOfSamples);
                            }
                        }

                        Interlocked.Add(ref frameRays, rayCount);
                        countdownEvent.Signal();
                    }, (startWidth, endWidth, startHeight, endHeight));
                }
            }

            countdownEvent.Wait();

            RaysPerFrame = frameRays;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        internal static void RenderCpu(int width, int height)
        {
            var frameRays = 0u;
            var invertedWidth = 1f / width;
            var invertedHeight = 1f / height;

            Parallel.For(0, height, y =>
            {
                var rayCount = 0u;
                var randomHelper = new RandomHelper((uint) (y ^ DateTime.Now.Millisecond));

                for (uint x = 0; x < width; x++)
                {
                    randomHelper.Seed(x);
                    if (randomHelper.GetRandomBetween(100, 0) < SkipPixelProbability)
                    {
                        continue;
                    }

                    var u = x * invertedWidth;
                    var v = y * invertedHeight;
                    var ray = Camera.GetRay(SceneParams.Camera, u, v);
                    var color = Vector4.Zero;
                    for (var i = 0; i < Settings.NumberOfSamples; i++)
                    {
                        color += GetColorForRay(ray, ref rayCount, ref randomHelper);
                    }

                    FrameBuffer[y * width + x] = new RgbaFloat(color / Settings.NumberOfSamples);
                }

                Interlocked.Add(ref frameRays, rayCount);
            });

            RaysPerFrame = frameRays;
        }
    }
}