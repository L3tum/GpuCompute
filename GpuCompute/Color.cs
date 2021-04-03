// ReSharper disable SuggestVarOrType_BuiltInTypes

using ComputeSharp;

namespace GpuCompute
{
    [AutoConstructor]
    public readonly partial struct Color
    {
        public readonly float Red;
        public readonly float Green;
        public readonly float Blue;

        // public static Color operator *(Color color, Color other)
        // {
        //     return new(color.Red * other.Red, color.Green * other.Green, color.Blue * other.Blue);
        // }
        //
        // public static Color operator *(Color color, float brightness)
        // {
        //     return new(color.Red * brightness, color.Green * brightness, color.Blue * brightness);
        // }
        //
        // public static Color operator +(Color color, Color other)
        // {
        //     return new(color.Red + other.Red, color.Green + other.Green, color.Blue + other.Blue);
        // }
        //
        // public static Color operator +(Color color, float brightness)
        // {
        //     return new(color.Red + brightness, color.Green + brightness, color.Blue + brightness);
        // }

        // public int GetRgb()
        // {
        //     uint redPart = (uint) (Red * 255);
        //     uint greenPart = (uint) (Green * 255);
        //     uint bluePart = (uint) (Blue * 255);
        //
        //     // Shift bits to right place
        //     redPart = (redPart << 16) & 0x00FF0000; //Shift red 16-bits and mask out other stuff
        //     greenPart = (greenPart << 8) & 0x0000FF00; //Shift Green 8-bits and mask out other stuff
        //     bluePart = bluePart & 0x000000FF; //Mask out anything not blue.
        //
        //     //0xFF000000 for 100% Alpha. Bitwise OR everything together.
        //     return (int) (0xFF000000 | redPart | greenPart | bluePart);
        // }

        // https://en.wikipedia.org/wiki/Grayscale#Luma_coding_in_video_systems
        // public float GetLuminance()
        // {
        //     return Red * 0.2126F + Green * 0.7152F + Blue * 0.0722F;
        // }

        // public static Color FromInt(int argb)
        // {
        //     int b = argb & 0xFF;
        //     int g = (argb >> 8) & 0xFF;
        //     int r = (argb >> 16) & 0xFF;
        //
        //     return new Color(r / 255F, g / 255F, b / 255F);
        // }

        // private static float Lerp(float a, float b, float t)
        // {
        //     return a + t * (b - a);
        // }
        //
        // public static Color Lerp(Color a, Color b, float t)
        // {
        //     return new(Lerp(a.Red, b.Red, t), Lerp(a.Green, b.Green, t), Lerp(a.Blue, b.Blue, t));
        // }
    }
}