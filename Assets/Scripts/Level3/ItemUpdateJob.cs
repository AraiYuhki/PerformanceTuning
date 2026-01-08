using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Xeon.Performance.Level3
{
    [BurstCompile]
    public struct ItemUpdateJob : IJobParallelFor
    {
        private const float PullDuration = 0.3f;
        private const float CollisionDistance = 0.75f; // Player Radius 0.5 + Item Radius 0.25

        public NativeArray<ItemData> ItemDatas;
        public NativeArray<AnimationData> AnimationDatas;

        public NativeArray<float3> Positions;
        public NativeArray<quaternion> Rotations;
        public NativeArray<float3> Scaled;
        public NativeArray<float4> SpriteRects;

        [ReadOnly] public float3 PlayerPosition;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float3 Scale;

        public void Execute(int index)
        {
            var itemData = ItemDatas[index];
            var animationData = AnimationDatas[index];
            if (!itemData.IsActive)
            {
                Positions[index] = new float3(0, -10000, 0);
                Rotations[index] = quaternion.identity;
                Scaled[index] = new float3(1f, 1f, 1f);
                return;
            }

            animationData.AnimationElapsed += DeltaTime;
            if (animationData.AnimationElapsed >= animationData.FrameDuration)
            {
                animationData.CurrentFrame = (animationData.CurrentFrame + 1) % animationData.TotalFrames;
                SpriteRects[index] = ComputeSpriteRect(animationData);
                animationData.AnimationElapsed = 0f;
            }
            if (itemData.IsPulling)
            {
                itemData.Elapsed += DeltaTime;
                var percent = itemData.Elapsed / PullDuration;
                if (percent >= 1f)
                {
                    Positions[index] = PlayerPosition;
                    itemData.IsActive = false;
                }
                else
                {
                    Positions[index] = math.lerp(itemData.StartPosition, PlayerPosition, percent);
                }
            }
            else
            {
                if (math.distance(PlayerPosition, Positions[index]) < CollisionDistance)
                {
                    itemData.StartPosition = Positions[index];
                    itemData.IsPulling = true;
                }
            }

            AnimationDatas[index] = animationData;
            ItemDatas[index] = itemData;
        }

        private float4 ComputeSpriteRect(AnimationData animationData)
        {
            float frameWidth = 1f / 4;
            float frameHeight = 1f / 12;
            int col = animationData.CurrentFrame % 4;
            return new float4(frameWidth, frameHeight, col * frameWidth, frameHeight * animationData.ColorIndex);
        }
    }
}