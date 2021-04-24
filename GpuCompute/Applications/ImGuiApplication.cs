using Veldrid;

namespace GpuCompute.Applications
{
    public class ImGuiApplication : Application
    {
        private CommandList commandList = null!;
        private ImGuiRenderer imGuiRenderer = null!;

        protected override void OnInitialize()
        {
            commandList = GraphicsDevice.ResourceFactory.CreateCommandList();
            CommandLists.Add(commandList);
            imGuiRenderer = new ImGuiRenderer(
                GraphicsDevice,
                GraphicsDevice.MainSwapchain.Framebuffer.OutputDescription,
                WIDTH,
                HEIGHT
            );
            Window.Resized += () => imGuiRenderer.WindowResized(Window.Width, Window.Height);
        }

        protected override void Update(InputSnapshot snapshot, float elapsedSeconds)
        {
            imGuiRenderer.Update(elapsedSeconds, snapshot);
        }

        protected override void Render()
        {
            commandList.Begin();
            commandList.SetFramebuffer(GraphicsDevice.MainSwapchain.Framebuffer);
            imGuiRenderer.Render(GraphicsDevice, commandList);
            commandList.End();
        }
    }
}