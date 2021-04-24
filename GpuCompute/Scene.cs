using System.Collections.Generic;
using System.Numerics;

namespace GpuCompute
{
    public struct Scene
    {
        public Camera Camera;
        public List<Solid> Solids;
        public Vector4 BackgroundColor;
    }
}