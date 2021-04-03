using ComputeSharp;

// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace GpuCompute
{
    public readonly struct Solid
    {
        public readonly int SolidType;
        public readonly float Radius;
        public readonly float RadiusSquared;
        public readonly Float3 Position;
        public readonly Float4 Color;
        public readonly float Reflectivity;
        public readonly float Emission;
        public readonly int SolidId;

        public Solid(SolidType solidType, float radius, Float3 position, Float4 color, float reflectivity,
            float emission, int solidId)
        {
            SolidType = (int) solidType;
            Radius = radius;
            RadiusSquared = radius * radius;
            Position = position;
            Color = color;
            Reflectivity = reflectivity;
            Emission = emission;
            SolidId = solidId;
        }
    }


    public enum SolidType
    {
        SPHERE = 0,
        PLANE = 1,
        BOX = 2
    }
}