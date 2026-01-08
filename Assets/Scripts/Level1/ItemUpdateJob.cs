using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace Xeon.Performance.Level1
{
    [BurstCompile]
    public struct ItemUpdateJob : IJobParallelForTransform
    {
        private const float PullDuration = 0.3f;
        private const float CollisionDistance = 0.75f; // Player Radius 0.5 + Item Radius 0.25
        private NativeArray<ItemData> itemDatas;
        private readonly Vector3 playerPosition;
        private readonly float deltaTime;

        public ItemUpdateJob(NativeArray<ItemData> itemDatas, Vector3 playerPosition, float deltaTime)
        {
            this.itemDatas = itemDatas;
            this.playerPosition = playerPosition;
            this.deltaTime = deltaTime;
        }

        public void Execute(int index, TransformAccess transform)
        {
            var itemData = itemDatas[index];
            if (!itemData.IsActive)
            {
                return;
            }

            // アニメーション更新
            itemData.AnimationElapsed += deltaTime;
            if (itemData.AnimationElapsed >= itemData.FrameDuration)
            {
                itemData.CurrentFrame = (itemData.CurrentFrame + 1) % itemData.TotalFrames;
                itemData.AnimationElapsed = 0f;
            }

            // 移動処理
            var currentPosition = transform.position;
            if (itemData.IsPulling)
            {
                itemData.Elapsed += deltaTime;
                var percent = itemData.Elapsed / PullDuration;
                if (percent >= 1f)
                {
                    transform.position = playerPosition;
                    itemData.IsActive = false;
                }
                else
                {
                    transform.position = Vector3.Lerp(itemData.StartPosition, playerPosition, percent);
                }
            }
            else
            {
                if (Vector3.Distance(playerPosition, currentPosition) < CollisionDistance)
                {
                    itemData.StartPosition = currentPosition;
                    itemData.IsPulling = true;
                }
            }

            itemDatas[index] = itemData;
        }
    }
}
