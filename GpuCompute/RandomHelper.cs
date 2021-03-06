using System.Runtime.CompilerServices;

namespace GpuCompute
{
    public struct RandomHelper
    {
        private uint state;

        public RandomHelper(uint state)
        {
            this.state = state;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        public void Seed(uint seed)
        {
            state ^= seed | 9823749;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        private void XorShift()
        {
            var stat = state;
            stat ^= stat << 13;
            stat ^= stat >> 17;
            stat ^= stat << 15;
            state = stat;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        public uint GetRandom()
        {
            XorShift();

            return state;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        public uint GetRandomBetween(uint max, uint min)
        {
            return GetRandom() % (max - min) + min;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        public float RandomFloat()
        {
            return GetRandom() * (1f / 4294967296f);
        }
    }
}