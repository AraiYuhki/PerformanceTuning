using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Xeon.Performance.Level2
{
    [BurstCompile]
    public struct ItemUpdateJob : IJobParallelFor
    {
        private const float PullDuration = 0.3f;
        private const float CollisionDistance = 0.75f; // Player Radius 0.5 + Item Radius 0.25

        public NativeArray<ItemData> ItemDatas;
        public NativeArray<float4x4> Matrices;

        [ReadOnly] public float3 PlayerPosition;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public float3 Scale;

        public void Execute(int index)
        {
            var itemData = ItemDatas[index];
            if (!itemData.IsActive)
            {
                // 非アクティブなアイテムは画面外に配置
                Matrices[index] = float4x4.TRS(new float3(0, -10000, 0), quaternion.identity, Scale);
                return;
            }

            // アニメーション更新
            itemData.AnimationElapsed += DeltaTime;
            if (itemData.AnimationElapsed >= itemData.FrameDuration)
            {
                itemData.CurrentFrame = (itemData.CurrentFrame + 1) % itemData.TotalFrames;
                itemData.SpriteRect = ComputeSpriteRect(itemData);
                itemData.AnimationElapsed = 0f;
            }

            // 移動処理
            if (itemData.IsPulling)
            {
                itemData.Elapsed += DeltaTime;
                var percent = itemData.Elapsed / PullDuration;
                if (percent >= 1f)
                {
                    itemData.Position = PlayerPosition;
                    itemData.IsActive = false;
                }
                else
                {
                    itemData.Position = math.lerp(itemData.StartPosition, PlayerPosition, percent);
                }
            }
            else
            {
                if (math.distance(PlayerPosition, itemData.Position) < CollisionDistance)
                {
                    itemData.StartPosition = itemData.Position;
                    itemData.IsPulling = true;
                }
            }

            Matrices[index] = float4x4.TRS(itemData.Position, quaternion.identity, Scale);
            ItemDatas[index] = itemData;
        }

        private Vector4 ComputeSpriteRect(ItemData itemData)
        {
            float frameWidth = 1f / 4;
            float frameHeight = 1f / 12;
            int col = itemData.CurrentFrame % 4;
            return new Vector4(frameWidth, frameHeight, col * frameWidth, frameHeight * itemData.ColorIndex);
        }
    }
}

