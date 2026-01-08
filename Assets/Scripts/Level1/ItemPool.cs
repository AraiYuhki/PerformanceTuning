using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Jobs;
using UnityEngine.Pool;
using Xeon.Performance.Common;
using Random = UnityEngine.Random;

namespace Xeon.Performance.Level1
{
    public class ItemPool : MonoBehaviour
    {
        private const int MaxCapacity = 100_0000;
        private const int AtlasColumns = 4;
        private const int AtlasRows = 12;
        private const int TotalFrames = AtlasColumns * AtlasRows;

        [SerializeField] private Item itemPrefab;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Player player;
        [SerializeField] private TMP_Text activeItemCountLabel;
        [SerializeField] private float frameDuration = 0.1f;

        private int nextId = 0;

        private ObjectPool<Item> itemPool;
        private NativeArray<ItemData> itemDatas = new NativeArray<ItemData>(MaxCapacity, Allocator.Persistent);
        private TransformAccessArray transformAccessArray;
        private List<Item> activeItems = new();

        private float elapsed = 0f;
        private const float SpawnInterval = 0.2f;
        private const int SpawnCountPerInterval = 100;

        private void Start()
        {
            transformAccessArray = new TransformAccessArray(MaxCapacity);
            itemPool = new ObjectPool<Item>(OnCreate, OnGet, OnRelease, OnItemDestroy, defaultCapacity: 1000,
                maxSize: MaxCapacity);
        }

        private void OnDestroy()
        {
            itemPool.Dispose();
            transformAccessArray.Dispose();
            itemDatas.Dispose();
        }

        private void Update()
        {
            var job = new ItemUpdateJob(itemDatas, player.transform.position, Time.deltaTime);
            var handle = job.Schedule(transformAccessArray);
            handle.Complete();

            var itemsToRelease = ListPool<Item>.Get();
            try
            {
                foreach (var item in activeItems)
                {
                    var itemData = itemDatas[item.Id];
                    if (!itemData.IsActive)
                    {
                        itemsToRelease.Add(item);
                    }
                    else
                    {
                        item.SetSpriteRect(AtlasColumns, AtlasRows, itemData.ColorIndex, itemData.CurrentFrame);
                    }
                }

                foreach (var item in itemsToRelease)
                {
                    itemPool.Release(item);
                }
            }
            finally
            {
                ListPool<Item>.Release(itemsToRelease);
            }

            activeItemCountLabel.text = $"{activeItems.Count}/{MaxCapacity}";

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
                for (var count = 0; count < spawnCount; count++)
            {
                itemPool.Get();
                }
                elapsed += SpawnInterval;
            }
        }

        private Item OnCreate()
        {
            var instance = Instantiate(itemPrefab, transform);
            instance.gameObject.SetActive(false);
            instance.Id = nextId;
            transformAccessArray.Add(instance.transform);
            nextId++;
            return instance;
        }

        private void OnGet(Item target)
        {
            var minBounds = Camera.main.ViewportToWorldPoint(Vector3.zero);
            var maxBounds = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));
            var randomX = Random.Range(minBounds.x, maxBounds.x);
            var randomY = Random.Range(minBounds.y, maxBounds.y);
            target.transform.position = new Vector3(randomX, randomY, -target.Id * 0.0001f);
            target.IsReleased = false;
            target.gameObject.SetActive(true);
            activeItems.Add(target);

            var startFrame = Random.Range(0, TotalFrames);
            target.SetSpriteRect(AtlasColumns, AtlasRows, 0, startFrame);

            var data = itemDatas[target.Id];
            data.Id = target.Id;
            data.ColorIndex = Random.Range(0, 12);
            data.Position = target.transform.position;
            data.IsActive = true;
            data.IsPulling = false;
            data.Elapsed = 0f;
            data.StartPosition = target.transform.position;
            data.CurrentFrame = startFrame;
            data.AnimationElapsed = 0f;
            data.FrameDuration = frameDuration;
            data.TotalFrames = TotalFrames;
            itemDatas[target.Id] = data;
        }

        private void OnRelease(Item target)
        {
            if (target.IsReleased)
                return;
            target.IsReleased = true;
            target.gameObject.SetActive(false);
            activeItems.Remove(target);
        }

        private void OnItemDestroy(Item target)
        {
            Destroy(target);
        }
    }
}
