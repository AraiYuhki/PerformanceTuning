using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;

namespace Xeon.Performance.Level2
{
    [BurstCompile]
    public struct ItemData
    {
        public bool IsPulling;
        public bool IsActive;
        public float3 StartPosition;
        public float3 Position;
        public float Elapsed;

        // アニメーション用
        public int ColorIndex;
        public int CurrentFrame;
        public float AnimationElapsed;
        public float FrameDuration;
        public int TotalFrames;

        public Vector4 SpriteRect;
    }
}

