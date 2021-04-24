namespace GpuCompute
{
    public struct RaytracerSettings
    {
        internal int MaxBounces;
        internal float MinimumIllumination;
        internal int NumberOfSamples;
        internal bool RandomizeBounceOnRoughness;
        internal bool DiffuseLightSampling;
        internal int DiffuseLightSamplingLight;
        internal bool DiffuseLightSamplingOneLight;
        internal bool RandomizeCameraRays;

        internal static RaytracerSettings Create()
        {
            RaytracerSettings raytracerSettings;
            raytracerSettings.MaxBounces = Constants.DEFAULT_MAX_BOUNCES;
            raytracerSettings.MinimumIllumination = Constants.DEFAULT_MINIMUM_ILLUMINATION;
            raytracerSettings.NumberOfSamples = Constants.DEFAULT_NUMBER_OF_SAMPLES;
            raytracerSettings.RandomizeBounceOnRoughness = Constants.DEFAULT_RANDOMIZE_BOUNCE;
            raytracerSettings.DiffuseLightSampling = Constants.DEFAULT_LIGHT_SAMPLING;
            raytracerSettings.DiffuseLightSamplingLight = Constants.DEFAULT_SAMPLE_LIGHT;
            raytracerSettings.DiffuseLightSamplingOneLight = Constants.DEFAULT_SAMPLE_ONE_LIGHT;
            raytracerSettings.RandomizeCameraRays = Constants.DEFAULT_RANDOMIZE_CAMERA_RAYS;

            return raytracerSettings;
        }
    }
}