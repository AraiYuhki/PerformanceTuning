using Unity.Burst;
using UnityEngine;

namespace Xeon.Performance.Level1
{
    [BurstCompile]
    public struct ItemData
    {
        public int Id;
        public bool IsPulling;
        public bool IsActive;
        public Vector3 StartPosition;
        public Vector3 Position;
        public float Elapsed;

        // アニメーション用
        public int ColorIndex;
        public int CurrentFrame;
        public float AnimationElapsed;
        public float FrameDuration;
        public int TotalFrames;
    }
}
