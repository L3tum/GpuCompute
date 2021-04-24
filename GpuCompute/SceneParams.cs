using System.Numerics;
using System.Runtime.InteropServices;

namespace GpuCompute
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SceneParams
    {
        public Camera Camera;
        public uint SphereCount;
        public Vector4 BackgroundColor;
    }
}