using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Xeon.Performance.Common;
using Random = UnityEngine.Random;
using TMPro;
using UnityEngine.InputSystem;


namespace Xeon.Performance.Level3
{
    public class ItemPool : MonoBehaviour
    {
        private const int MaxCapacity = 100_0000;
        private const int AtlasColumns = 4;
        private const int AtlasRows = 12;
        private const int TotalFrames = AtlasColumns * AtlasRows;

        [SerializeField] private Mesh quadMesh;
        [SerializeField] private Material instanceMaterial;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Player player;
        [SerializeField] private TMP_Text activeItemCountLabel;
        [SerializeField] private float frameDuration = 0.1f;
        [SerializeField] private Vector3 itemScale = new Vector3(0.5f, 0.5f, 1f);

        private NativeArray<ItemData> itemDatas;
        private NativeArray<AnimationData> animationDatas;
        private NativeArray<float3> positions;
        private NativeArray<quaternion> rotations;
        private NativeArray<float3> scales;
        private NativeArray<float4> spriteRects;
        private NativeList<int> activeIndices;

        private GraphicsBuffer positionsBuffer;
        private GraphicsBuffer rotationsBuffer;
        private GraphicsBuffer scalesBuffer;
        private GraphicsBuffer spriteRectsBuffer;

        private static readonly Bounds DrawBounds = new Bounds(Vector3.zero, new Vector3(10000, 10000, 10));

        private float elapsed = 0f;
        private const float SpawnInterval = 0.2f;
        private const int SpawnCountPerInterval = 100;

        private int activeCount = 0;
        private int nextSpawnIndex = 0;

        private void Start()
        {
            itemDatas = new NativeArray<ItemData>(MaxCapacity, Allocator.Persistent);
            animationDatas = new NativeArray<AnimationData>(MaxCapacity, Allocator.Persistent);
            positions = new NativeArray<float3>(MaxCapacity, Allocator.Persistent);
            rotations = new NativeArray<quaternion>(MaxCapacity, Allocator.Persistent);
            scales = new NativeArray<float3>(MaxCapacity, Allocator.Persistent);
            spriteRects = new NativeArray<float4>(MaxCapacity, Allocator.Persistent);
            activeIndices = new NativeList<int>(MaxCapacity, Allocator.Persistent);

            positionsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MaxCapacity, sizeof(float) * 3);
            rotationsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MaxCapacity, sizeof(float) * 4);
            scalesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MaxCapacity, sizeof(float) * 3);
            spriteRectsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MaxCapacity, sizeof(float) * 4);

            instanceMaterial.SetBuffer("_Positions", positionsBuffer);
            instanceMaterial.SetBuffer("_Rotations", rotationsBuffer);
            instanceMaterial.SetBuffer("_Scales", scalesBuffer);
            instanceMaterial.SetBuffer("_SpriteRects", spriteRectsBuffer);

            for (int i = 0; i < MaxCapacity; i++)
            {
                itemDatas[i] = new ItemData() {
                    IsActive = false,
                };
                animationDatas[i] = new AnimationData();
                positions[i] = new float3(0, -10000, 0);
                rotations[i] = quaternion.identity;
                scales[i] = new float3(1f, 1f, 1f);
            }
        }

        private void OnDestroy()
        {
            if (itemDatas.IsCreated) itemDatas.Dispose();
            if (animationDatas.IsCreated) animationDatas.Dispose();
            if (positions.IsCreated) positions.Dispose();
            if (rotations.IsCreated) rotations.Dispose();
            if (scales.IsCreated) scales.Dispose();
            if (spriteRects.IsCreated) spriteRects.Dispose();
            if (activeIndices.IsCreated) activeIndices.Dispose();

            positionsBuffer?.Dispose();
            rotationsBuffer?.Dispose();
            scalesBuffer?.Dispose();
            spriteRectsBuffer?.Dispose();
        }

        private void Update()
        {
            var job = new ItemUpdateJob() {
                ItemDatas = itemDatas,
                AnimationDatas = animationDatas,
                Positions = positions,
                Rotations = rotations,
                Scaled = scales,
                SpriteRects = spriteRects,
                PlayerPosition = player.transform.position,
                DeltaTime = Time.deltaTime,
                Scale = itemScale,
            };

            var handle = job.Schedule(nextSpawnIndex, 64);
            handle.Complete();

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

            DrawAllItems();

            activeItemCountLabel.text = $"{activeCount}/{MaxCapacity}";
            if (elapsed > 0f)
            {
                elapsed -= Time.deltaTime;
                return;
            }
            if (Keyboard.current.spaceKey.isPressed)
            {
                var spawnCount = SpawnCountPerInterval;
                if (Keyboard.current.leftShiftKey.isPressed)
                {
                    spawnCount *= 10;
                }
                else if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.leftCommandKey.isPressed)
                {
                    spawnCount *= 100;
                }
                for (int count = 0; count < spawnCount; count++)
                {
                    SpawnItem();
                }
                elapsed += SpawnInterval;
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
            itemDatas[index] = new ItemData()
            {
                IsActive = true,
                IsPulling = false,
                StartPosition = new float3(0, -10000, 0),
                Elapsed = 0f,
            };

            animationDatas[index] = new AnimationData()
            {
                ColorIndex = Random.Range(0, AtlasRows),
                CurrentFrame = Random.Range(0, TotalFrames),
                AnimationElapsed = 0f,
                FrameDuration = frameDuration,
                TotalFrames = TotalFrames,
            };
            positions[index] = new float3(Random.Range(minBounds.x, maxBounds.x), Random.Range(minBounds.y, maxBounds.y), 0f);
            rotations[index] = quaternion.RotateZ(Random.Range(0f, math.PI * 2f));
            scales[index] = itemScale;
            spriteRects[index] = ComputeSpriteRect(animationDatas[index]);
        }

        private float4 ComputeSpriteRect(AnimationData animationData)
        {
            const float frameWidth = 1f / AtlasColumns;
            const float frameHeight = 1f / AtlasRows;
            int col = animationData.CurrentFrame % AtlasColumns;
            return new float4(frameWidth, frameHeight, col * frameWidth, frameHeight * animationData.ColorIndex);
        }

        private void DrawAllItems()
        {
            if (nextSpawnIndex == 0) return;

            positionsBuffer.SetData(positions, 0, 0, nextSpawnIndex);
            rotationsBuffer.SetData(rotations, 0, 0, nextSpawnIndex);
            scalesBuffer.SetData(scales, 0, 0, nextSpawnIndex);
            spriteRectsBuffer.SetData(spriteRects, 0, 0, nextSpawnIndex);

            Graphics.DrawMeshInstancedProcedural(quadMesh, 0, instanceMaterial, DrawBounds, nextSpawnIndex);
        }
    }
}