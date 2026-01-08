using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;

namespace Xeon.Performance.Level3
{
    [BurstCompile]
    public struct ItemData
    {
        public bool IsPulling;
        public bool IsActive;
        public float3 StartPosition;
        public float3 Position;
        public float Elapsed;
    }

    [BurstCompile]
    public struct AnimationData
    {
        public int ColorIndex;
        public int CurrentFrame;
        public float AnimationElapsed;
        public float FrameDuration;
        public int TotalFrames;
    }
}