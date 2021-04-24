using System.Collections.Generic;
using System.Numerics;

namespace GpuCompute.Scenes
{
    public class BookScene : SceneGenerator
    {
        public Scene GetScene(int width, int height)
        {
            Scene scene;

            var camPos = new Vector3(9.5f, 2f, 2.5f);
            var lookAt = new Vector3(3, 0.5f, 0.65f);
            scene.Camera = Camera.Create(
                camPos,
                lookAt,
                Vector3.UnitY,
                25f,
                width / (float) height,
                0.01f,
                (camPos - lookAt).Length()
            );
            scene.Solids = new List<Solid>();
            scene.Solids.Add(new Solid(
                SolidType.SPHERE,
                1000f,
                new Vector3(0, -1000, 0),
                new Vector4(1f, 1f, 0f, 1f),
                0.0f,
                1f,
                0f,
                0));
            scene.Solids.Add(new Solid(
                SolidType.SPHERE,
                1f,
                new Vector3(0, 1, 0),
                new Vector4(0f, 0f, 1f, 1f),
                0f,
                0f,
                0f,
                1
            ));
            scene.Solids.Add(new Solid(
                SolidType.SPHERE,
                1f,
                new Vector3(-4, 1, 0),
                new Vector4(0f, 1f, 0f, 1f),
                0f,
                0f,
                0f,
                2
            ));
            scene.Solids.Add(new Solid(
                SolidType.SPHERE,
                1f,
                new Vector3(4, 1, 0),
                new Vector4(1f, 0f, 0f, 1f),
                0f,
                0f,
                0f,
                3
            ));

            scene.BackgroundColor = Vector4.Zero;

            return scene;
        }
    }
}