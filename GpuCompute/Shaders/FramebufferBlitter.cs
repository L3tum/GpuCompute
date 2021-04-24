using System.Numerics;
using ShaderGen;

// ReSharper disable SuggestVarOrType_SimpleTypes

// ReSharper disable SuggestVarOrType_BuiltInTypes

[assembly:
    ShaderSet("FramebufferBlitter", "GpuCompute.Shaders.FramebufferBlitter.VS",
        "GpuCompute.Shaders.FramebufferBlitter.FS")]

namespace GpuCompute.Shaders
{
    public class FramebufferBlitter
    {
        [VertexShader]
        public FragmentInput VS()
        {
            uint vertexID = ShaderBuiltins.VertexID;
            FragmentInput output;
            output.TexCoords = new Vector2((vertexID << 1) & 2u, vertexID & 2u);
            output.Position = new Vector4(output.TexCoords.XY() * 2.0f - new Vector2(1.0f), 0.0f, 1.0f);
            return output;
        }
        
        public Texture2DResource SourceTexture = null!;
        public SamplerResource SourceSampler = null!;

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            Vector4 color = ShaderBuiltins.Sample(SourceTexture, SourceSampler, input.TexCoords);
            return new Vector4(ToSrgb(color.XYZ()), 1f);
        }

        private static Vector3 ToSrgb(Vector3 color)
        {
            color = Vector3.Max(color, Vector3.Zero);
            return Vector3.Max(1.055f * ShaderBuiltins.Pow(color, new Vector3(0.41666667f)) - new Vector3(0.055f),
                Vector3.Zero);
        }
    }

    public struct FragmentInput
    {
        [SystemPositionSemantic] public Vector4 Position;
        [TextureCoordinateSemantic] public Vector2 TexCoords;
    }
}