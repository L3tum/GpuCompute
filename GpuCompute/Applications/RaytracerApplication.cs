using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GpuCompute.Scenes;
using ImGuiNET;
using Veldrid;
using Veldrid.SPIRV;

namespace GpuCompute.Applications
{
    public class RaytracerApplication : SettingsApplication
    {
        private static readonly SceneGenerator[] Scenes =
        {
            new BookScene(),
            new ToyPathTracerScene()
        };

        private CancellationTokenSource? cancellationTokenSource;

        private CommandList commandList = null!;
        private Pipeline computePipeline = null!;
        private ResourceSet computeSet = null!;
        private Pipeline graphicsPipeline = null!;
        private ResourceSet graphicsSet = null!;
        private bool isResizing;
        private DeviceBuffer rayCountBuffer = null!;
        private DeviceBuffer rayCountReadback = null!;
        private bool renderOnce = true;
        private Task? renderThread;
        private DeviceBuffer sceneParamsBuffer = null!;
        private DeviceBuffer solidsBuffer = null!;
        private TextureView textureView = null!;
        private Texture transferTexture = null!;

        private void Dispose()
        {
            graphicsPipeline.Dispose();
            graphicsSet.Dispose();                  
            textureView.Dispose();
            transferTexture.Dispose();
            commandList.Dispose();
        }

        private void OnResized()
        {
            isResizing = true;
            if (transferTexture == null || transferTexture.Width != Window.Width ||
                transferTexture.Height != Window.Height)
            {
                if (transferTexture != null)
                {
                    Dispose();
                }

                CreateDeviceResources();
                Raytracer.Width = Window.Width;
                Raytracer.Height = Window.Height;
                Raytracer.InvertedWidth = 1f / Window.Width;
                Raytracer.InvertedHeight = 1f / Window.Height;
                Raytracer.Rows = (int) Math.Round(Window.Height / (float) Raytracer.BlockHeight, 0,
                    MidpointRounding.ToPositiveInfinity);
                Raytracer.Columns = (int) Math.Round(Window.Width / (float) Raytracer.BlockWidth, 0,
                    MidpointRounding.ToPositiveInfinity);
            }

            isResizing = false;
        }

        protected override void OnInitialize()
        {
            var scene = Scenes[1].GetScene(WIDTH, HEIGHT);
            Raytracer.LoadScene(scene);
            Raytracer.FrameBuffer = new RgbaFloat[WIDTH * HEIGHT];
            CreateDeviceResources();
            CommandLists.Add(commandList);
            Window.Resized += OnResized;
            base.OnInitialize();
            OnResized();
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected override void Update(InputSnapshot snapshot, float elapsedSeconds)
        {
            base.Update(snapshot, elapsedSeconds);
            var frameRate = 1f / ElapsedSecondsBetweenRenders;
            var tickRate = 1f / elapsedSeconds;
            var rate = Raytracer.RaysPerFrame * frameRate;
            var millionRate = rate / 1_000_000;
            var budget = Stopwatch.Frequency / TargetTicksPerFrame;
            Window.Title =
                $"{millionRate:F2} MRays / sec. | {Raytracer.RaysPerFrame / 1_000_000:F2} MRays / Frame | {frameRate:F2} FPS | {tickRate:F2} TPS | {MillisecondsPerLogic:F0}ms / Update | {MillisecondsPerRender:F0}ms / Render | Budget: {budget:F0}ms / Frame";

            if (ImGui.Begin("Debug"))
            {
                if (ImGui.BeginMenu("Raytracer Debug"))
                {
                    if (ImGui.Button("Clear Framebuffer"))
                    {
                        Array.Clear(Raytracer.FrameBuffer, 0, Raytracer.FrameBuffer.Length);
                    }

                    ImGui.Checkbox("Render once on button press", ref renderOnce);

                    if (renderOnce && ImGui.Button(
                        renderThread != null && !renderThread.IsCompleted && !renderThread.IsCanceled
                            ? "Rendering"
                            : "Render Once"))
                    {
                        if (renderThread != null && !renderThread.IsCompleted && !renderThread.IsCanceled)
                        {
                            cancellationTokenSource?.Cancel();
                        }

                        if (renderThread == null || renderThread.IsCompleted || renderThread.IsCanceled)
                        {
                            cancellationTokenSource = new CancellationTokenSource(100);
                            renderThread = Task.Run(Raytracer.RenderCpuNew,
                                cancellationTokenSource.Token);
                        }
                    }
                }
            }

            foreach (var keyEvent in snapshot.KeyEvents)
            {
                if (keyEvent.Down && keyEvent.Key == Key.D)
                {
                    Raytracer.SceneParams.Camera.Origin.X += 1f;
                    Raytracer.SceneParams.Camera.LookAt.X += 1f;
                    Camera.Recalculate(ref Raytracer.SceneParams.Camera);
                }
                else if (keyEvent.Down && keyEvent.Key == Key.A)
                {
                    Raytracer.SceneParams.Camera.Origin.X -= 1f;
                    Raytracer.SceneParams.Camera.LookAt.X -= 1f;
                    Camera.Recalculate(ref Raytracer.SceneParams.Camera);
                }
            }

            Raytracer.RenderSettings();
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected override void Render()
        {
            if (isResizing)
            {
                return;
            }

            commandList.Begin();

            if (DrawModeCpu)
            {
                if (!renderOnce)
                {
                    Raytracer.RenderCpuNew();
                }

                if (!isResizing)
                {
                    unsafe
                    {
                        fixed (RgbaFloat* pixelDataPtr = Raytracer.FrameBuffer)
                        {
                            GraphicsDevice.UpdateTexture(transferTexture, (IntPtr) pixelDataPtr,
                                (uint) (transferTexture.Width * transferTexture.Height * (uint) sizeof(RgbaFloat)), 0, 0, 0,
                                (uint) transferTexture.Width, (uint) transferTexture.Height, 1, 0, 0);
                        }
                    }
                }
            }
            else
            {
                commandList.ClearColorTarget(0, RgbaFloat.Black);
            }

            commandList.SetFramebuffer(GraphicsDevice.MainSwapchain.Framebuffer);
            commandList.SetPipeline(graphicsPipeline);
            commandList.SetGraphicsResourceSet(0, graphicsSet);
            commandList.Draw(3);
            commandList.End();

            if (!DrawModeCpu)
            {
                var rayCountView = GraphicsDevice.Map<uint>(rayCountReadback, MapMode.Read);
                var rPF = rayCountView[0];
                GraphicsDevice.Unmap(rayCountReadback);
            }

            base.Render();
        }

        private void CreateDeviceResources()
        {
            ResourceFactory factory = GraphicsDevice.ResourceFactory;
            commandList = factory.CreateCommandList();
            transferTexture = factory.CreateTexture(TextureDescription.Texture2D((uint) Window.Width,
                (uint) Window.Height, 1, 1,
                PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled | TextureUsage.Storage));
            textureView = factory.CreateTextureView(transferTexture);

            ResourceLayout graphicsLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SourceTexture", ResourceKind.TextureReadOnly,
                        ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SourceSampler", ResourceKind.Sampler, ShaderStages.Fragment)
                )
            );
            graphicsSet =
                factory.CreateResourceSet(new ResourceSetDescription(graphicsLayout, textureView,
                    GraphicsDevice.LinearSampler));

            graphicsPipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    Array.Empty<VertexLayoutDescription>(),
                    factory.CreateFromSpirv(new ShaderDescription(ShaderStages.Vertex,
                            LoadShaderBytes("FramebufferBlitter-vertex"), "main"),
                        new ShaderDescription(ShaderStages.Fragment,
                            LoadShaderBytes("FramebufferBlitter-fragment"), "main"))
                ),
                graphicsLayout,
                GraphicsDevice.MainSwapchain.Framebuffer.OutputDescription
            ));

            if (DrawModeCpu)
            {
                return;
            }

            solidsBuffer = factory.CreateBuffer(
                new BufferDescription(
                    (uint) Unsafe.SizeOf<Solid>() * Raytracer.SceneParams.SphereCount,
                    BufferUsage.StructuredBufferReadOnly,
                    (uint) Unsafe.SizeOf<Solid>()
                ));
            GraphicsDevice.UpdateBuffer(solidsBuffer, 0, Raytracer.Solids);

            sceneParamsBuffer = factory.CreateBuffer(
                new BufferDescription(
                    (uint) Unsafe.SizeOf<SceneParams>(),
                    BufferUsage.UniformBuffer
                ));
            GraphicsDevice.UpdateBuffer(sceneParamsBuffer, 0, Raytracer.SceneParams);

            rayCountBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.StructuredBufferReadWrite, 4));
            rayCountReadback = factory.CreateBuffer(new BufferDescription(16, BufferUsage.Staging));

            ResourceLayout computeLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Solids", ResourceKind.StructuredBufferReadOnly,
                    ShaderStages.Compute),
                new ResourceLayoutElementDescription("Output", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                new ResourceLayoutElementDescription("Params", ResourceKind.UniformBuffer, ShaderStages.Compute),
                new ResourceLayoutElementDescription("RayCount", ResourceKind.StructuredBufferReadWrite,
                    ShaderStages.Compute)));
            computeSet = factory.CreateResourceSet(new ResourceSetDescription(computeLayout,
                solidsBuffer,
                textureView,
                sceneParamsBuffer,
                rayCountBuffer));

            computePipeline = factory.CreateComputePipeline(new ComputePipelineDescription(
                factory.CreateShader(new ShaderDescription(ShaderStages.Compute, LoadShaderBytes("RayTracer-compute"),
                    "CS")),
                computeLayout,
                16,
                16,
                1));
        }
    }
}