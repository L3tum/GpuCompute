using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
// ReSharper disable SuggestVarOrType_SimpleTypes

namespace GpuCompute
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Camera
    {
        public Vector3 Origin;
        public float VFov;
        public Vector3 LookAt;
        public float AspectRatio;
        private Vector3 LowerLeftCorner;
        public float Aperture;
        public Vector3 Horizontal;
        public float FocusDistance;
        public Vector3 Vertical;
        private float LensRadius;
        public Vector3 Up;
        private RandomHelper randomHelper;
        private Vector3 Right;

        public static Camera Create(Vector3 origin, Vector3 lookAt, Vector3 up, float vfov, float aspect,
            float aperture, float focusDist)
        {
            Camera cam = new Camera();
            cam.LensRadius = aperture / 2f;
            cam.Origin = origin;
            cam.LookAt = lookAt;
            cam.Up = up;
            cam.VFov = vfov;
            cam.AspectRatio = aspect;
            cam.Aperture = aperture;
            cam.FocusDistance = focusDist;
            cam.randomHelper = new RandomHelper(666);
            Recalculate(ref cam);
            return cam;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        public static Ray GetRay(Camera cam, float s, float t)
        {
            Vector3 rd = cam.LensRadius * Vector3.One;
            Vector3 offset = cam.Right * rd.X + cam.Up * rd.Y;
            return Ray.Create(
                cam.Origin + offset,
                cam.LowerLeftCorner + s * cam.Horizontal + t * cam.Vertical - cam.Origin - offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void Recalculate(ref Camera camera)
        {
            var theta = camera.VFov * MathF.PI / 180f;
            var halfHeight = MathF.Tan(theta / 2f);
            var halfWidth = camera.AspectRatio * halfHeight;
            camera.LensRadius = camera.Aperture / 2f;
            var lookDirection = Vector3.Normalize(camera.Origin - camera.LookAt);
            camera.Right = Vector3.Normalize(Vector3.Cross(camera.Up, lookDirection));
            camera.Up = Vector3.Cross(lookDirection, camera.Right);
            camera.LowerLeftCorner = camera.Origin -
                                     halfWidth * camera.FocusDistance * camera.Right -
                                     halfHeight * camera.FocusDistance * camera.Up -
                                     camera.FocusDistance * lookDirection;
            camera.Horizontal = 2 * halfWidth * camera.FocusDistance * camera.Right;
            camera.Vertical = 2 * halfHeight * camera.FocusDistance * camera.Up;
        }
        
        // TODO: Replace (and add) hit functions
        // TODO: Roughness for bouncing
    }
}