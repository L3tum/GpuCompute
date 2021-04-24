using System.Collections.Generic;
using System.Numerics;

namespace GpuCompute.Scenes
{
    public class ToyPathTracerScene : SceneGenerator
    {
        public Scene GetScene(int width, int height)
        {
            Scene scene;

            var lookFrom = new Vector3(0, 2, 3);
            var lookAt = new Vector3(0, 0, 0);
            float distToFocus = 3;
            var aperture = 0.1f;
            aperture *= 0.2f;

            scene.Camera = Camera.Create(
                lookFrom,
                lookAt,
                Vector3.UnitY,
                60,
                (float) width / height,
                aperture,
                distToFocus);
            scene.Solids = new List<Solid>
            {
                Solid.CreateSphere(100, new Vector3(0, -100.5f, -1), new Vector3(0.8f), 0.0f, 0f, 0.5f, 0),
                Solid.CreateSphere(0.5f, new Vector3(2, 0, -1), new Vector3(0.8f, 0.4f, 0.4f), 0f, 0f, 0.5f, 1),
                Solid.CreateSphere(0.5f, new Vector3(0, 0, -1), new Vector3(0.4f, 0.8f, 0.4f), 0.0f, 0.0f, 0.5f, 2),
                Solid.CreateSphere(0.3f, new Vector3(-1.5f, 1.5f, 0f), new Vector3(1f, 1f, 1f),
                    0.0f, 10f, 0f, 3),
                Solid.CreateSphere(0.3f, new Vector3(1.5f, 1.5f, -2f), new Vector3(0.8f, 0.8f, 0.2f), 0.0f, 5f, 0f, 4)
            };
            scene.BackgroundColor = Vector4.Zero;

            return scene;
        }
    }
}