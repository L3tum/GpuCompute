using ImGuiNET;
using Veldrid;

namespace GpuCompute.Applications
{
    public class SettingsApplication : ImGuiApplication
    {
        private int targetFps = Constants.DEFAULT_TARGET_FRAMERATE;
        private int targetTps = Constants.DEFAULT_TARGET_UPDATERATE;
        
        protected override void Update(InputSnapshot snapshot, float elapsedSeconds)
        {
            base.Update(snapshot, elapsedSeconds);

            if (targetFps >= 1)
            {
                TargetTicksPerFrame = System.Diagnostics.Stopwatch.Frequency / targetFps;
            }

            if (targetTps >= 10 && targetTps > targetFps)
            {
                TargetTicksPerUpdate = System.Diagnostics.Stopwatch.Frequency / targetTps;
            }

            if (ImGui.Begin("Settings"))
            {
                if (ImGui.BeginMenu("Application Settings"))
                {
                    ImGui.InputInt("Target FPS", ref targetFps);
                    ImGui.InputInt("Target TPS", ref targetTps);
                }
            }
        }
    }
}