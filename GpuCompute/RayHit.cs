using ComputeSharp;

namespace GpuCompute
{ 
    public struct RayHit
    {
        public Float3 HitPosition;
        public Float3 Normal;
        public Solid Solid;
    }
}