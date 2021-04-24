namespace GpuCompute.Scenes
{
    public interface SceneGenerator
    {
        public Scene GetScene(int width, int height);
    }
}