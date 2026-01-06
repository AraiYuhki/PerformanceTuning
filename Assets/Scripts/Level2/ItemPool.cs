using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Xeon.Performance.Common;
using Random = UnityEngine.Random;

namespace Xeon.Performance.Level2
{
    public class ItemPool : MonoBehaviour
    {
        private const int MaxCapacity = 100_0000;
        private const int AtlasColumns = 4;
        private const int AtlasRows = 12;
        private const int TotalFrames = AtlasColumns * AtlasRows;
        private const int BatchSize = 1023; // DrawMeshInstanced の最大インスタンス数

        [SerializeField] private Mesh quadMesh;
        [SerializeField] private Material instanceMaterial;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Player player;
        [SerializeField] private TMP_Text activeItemCountLabel;
        [SerializeField] private float frameDuration = 0.1f;
        [SerializeField] private Vector3 itemScale = new Vector3(0.5f, 0.5f, 1f);

        private NativeArray<ItemData> itemDatas;
        private NativeArray<float4x4> matrices;
        private NativeList<int> activeIndices;

        private Matrix4x4[] batchMatrices;
        private Vector4[] batchSpriteRects;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int SpriteRectId = Shader.PropertyToID("_SpriteRect");

        private float elapsed = 0f;
        private const float SpawnInterval = 0.5f;
        private const int SpawnCountPerInterval = 100;

        private int activeCount = 0;
        private int nextSpawnIndex = 0;

        private void Start()
        {
            itemDatas = new NativeArray<ItemData>(MaxCapacity, Allocator.Persistent);
            matrices = new NativeArray<float4x4>(MaxCapacity, Allocator.Persistent);
            activeIndices = new NativeList<int>(MaxCapacity, Allocator.Persistent);

            batchMatrices = new Matrix4x4[BatchSize];
            batchSpriteRects = new Vector4[BatchSize];
            propertyBlock = new MaterialPropertyBlock();

            // 全てのアイテムを非アクティブで初期化
            for (int i = 0; i < MaxCapacity; i++)
            {
                var data = new ItemData
                {
                    IsActive = false,
                    Position = new float3(0, -10000, 0)
                };
                itemDatas[i] = data;
                matrices[i] = float4x4.TRS(data.Position, quaternion.identity, (float3)itemScale);
            }
        }

        private void OnDestroy()
        {
            if (itemDatas.IsCreated) itemDatas.Dispose();
            if (matrices.IsCreated) matrices.Dispose();
            if (activeIndices.IsCreated) activeIndices.Dispose();
        }

        private void Update()
        {
            // Job でアイテムを更新
            var job = new ItemUpdateJob
            {
                ItemDatas = itemDatas,
                Matrices = matrices,
                PlayerPosition = player.transform.position,
                DeltaTime = Time.deltaTime,
                Scale = itemScale
            };
            var handle = job.Schedule(nextSpawnIndex, 64);
            handle.Complete();

            // 非アクティブになったアイテムをカウント
            activeCount = 0;
            activeIndices.Clear();
            for (int i = 0; i < nextSpawnIndex; i++)
            {
                if (itemDatas[i].IsActive)
                {
                    activeIndices.Add(i);
                    activeCount++;
                }
            }

            // 描画
            DrawAllItems();

            activeItemCountLabel.text = $"{activeCount}/{MaxCapacity}";

            // スポーン処理
            elapsed += Time.deltaTime;
            if (elapsed >= SpawnInterval)
            {
                elapsed = 0f;
                for (int count = 0; count < SpawnCountPerInterval; count++)
                {
                    SpawnItem();
                }
            }
        }

        private void SpawnItem()
        {
            if (nextSpawnIndex >= MaxCapacity)
            {
                // 非アクティブなスロットを探して再利用
                for (int i = 0; i < nextSpawnIndex; i++)
                {
                    if (!itemDatas[i].IsActive)
                    {
                        ActivateItem(i);
                        return;
                    }
                }
                return; // 空きスロットなし
            }

            ActivateItem(nextSpawnIndex);
            nextSpawnIndex++;
        }

        private void ActivateItem(int index)
        {
            var minBounds = mainCamera.ViewportToWorldPoint(Vector3.zero);
            var maxBounds = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));
            var randomX = Random.Range(minBounds.x, maxBounds.x);
            var randomY = Random.Range(minBounds.y, maxBounds.y);
            var position = new float3(randomX, randomY, 0f);

            var startFrame = Random.Range(0, TotalFrames);

            var data = new ItemData
            {
                IsActive = true,
                IsPulling = false,
                Position = position,
                StartPosition = position,
                Elapsed = 0f,
                ColorIndex = Random.Range(0, AtlasRows),
                CurrentFrame = startFrame,
                AnimationElapsed = 0f,
                FrameDuration = frameDuration,
                TotalFrames = TotalFrames
            };

            itemDatas[index] = data;
            matrices[index] = float4x4.TRS(position, quaternion.identity, (float3)itemScale);
        }

        private void DrawAllItems()
        {
            int count = activeIndices.Length;
            int batchStart = 0;

            while (batchStart < count)
            {
                int batchCount = Mathf.Min(BatchSize, count - batchStart);

                for (int i = 0; i < batchCount; i++)
                {
                    int dataIndex = activeIndices[batchStart + i];
                    batchMatrices[i] = matrices[dataIndex];

                    var itemData = itemDatas[dataIndex];
                    batchSpriteRects[i] = itemData.SpriteRect;
                }

                propertyBlock.SetVectorArray(SpriteRectId, batchSpriteRects);
                Graphics.DrawMeshInstanced(quadMesh, 0, instanceMaterial, batchMatrices, batchCount, propertyBlock);

                batchStart += batchCount;
            }
        }
    }
}

