using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace GpuCompute
{
    public abstract class Application
    {
        protected const int WIDTH = 1920;
        protected const int HEIGHT = 1080;

        protected readonly List<CommandList> CommandLists;

        protected readonly bool DrawModeCpu = false;
        protected float ElapsedSecondsBetweenRenders;
        protected Stopwatch FrameStopwatch = null!;

        protected GraphicsDevice GraphicsDevice = null!;
        protected Stopwatch LogicStopwatch = null!;
        protected long MillisecondsPerLogic;
        protected long MillisecondsPerRender;

        // 60 FPS
        protected long TargetTicksPerFrame = Stopwatch.Frequency / 60;

        // 120 TPS
        protected long TargetTicksPerUpdate = Stopwatch.Frequency / 120;
        protected Sdl2Window Window = null!;

        protected Application()
        {
            CommandLists = new List<CommandList>();
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Run()
        {
            var backend = GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan)
                ? GraphicsBackend.Vulkan
                : VeldridStartup.GetPlatformDefaultBackend();

            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(100, 100, WIDTH, HEIGHT, WindowState.Normal, "GPU Compute"),
                new GraphicsDeviceOptions(false, null, false),
                backend,
                out Window,
                out GraphicsDevice
            );

            Window.Resized += () => GraphicsDevice.ResizeMainWindow((uint) Window.Width, (uint) Window.Height);

            OnInitialize();

            FrameStopwatch = Stopwatch.StartNew();
            LogicStopwatch = Stopwatch.StartNew();

            while (Window.Exists)
            {
                if (LogicStopwatch.ElapsedTicks >= TargetTicksPerUpdate)
                {
                    var elapsedSeconds = LogicStopwatch.ElapsedMilliseconds / 1000f;
                    LogicStopwatch.Restart();
                    var snapshot = Window.PumpEvents();
                    Update(snapshot, elapsedSeconds);
                    MillisecondsPerLogic = LogicStopwatch.ElapsedMilliseconds;
                }

                if (Window.Exists && FrameStopwatch.ElapsedTicks >= TargetTicksPerFrame)
                {
                    ElapsedSecondsBetweenRenders = FrameStopwatch.ElapsedMilliseconds / 1000f;
                    FrameStopwatch.Restart();
                    RenderFrame();
                    MillisecondsPerRender = FrameStopwatch.ElapsedMilliseconds;
                }
            }

            GraphicsDevice.Dispose();
        }

        private void RenderFrame()
        {
            Render();

            foreach (var commandList in CommandLists)
            {
                GraphicsDevice.SubmitCommands(commandList);
            }

            GraphicsDevice.SwapBuffers();
        }

        protected byte[] LoadShaderBytes(string name)
        {
            string extension;
            switch (GraphicsDevice.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                    extension = "hlsl";
                    break;
                case GraphicsBackend.Vulkan:
                    extension = "450.glsl";
                    break;
                case GraphicsBackend.OpenGL:
                    extension = "330.glsl";
                    break;
                case GraphicsBackend.Metal:
                    extension = "metal";
                    break;
                case GraphicsBackend.OpenGLES:
                    extension = "300.glsles";
                    break;
                default: throw new InvalidOperationException();
            }

            return File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", $"{name}.{extension}"));
        }

        protected abstract void Render();

        protected abstract void Update(InputSnapshot snapshot, float elapsedSeconds);

        protected abstract void OnInitialize();
    }
}