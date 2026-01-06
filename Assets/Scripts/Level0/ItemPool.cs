using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace Xeon.Performance.Level0
{
    public class ItemPool : MonoBehaviour
    {
        private const int MaxCapacity = 100_0000;
        [SerializeField] private Item[] itemPrefabs;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private TMP_Text activeItemCountLabel;
        [SerializeField] private Player player;

        private ObjectPool<Item>[] itemPools;
        private List<Item>[] activeItems;

        private float elapsed = 0f;
        private const float SpawnInterval = 0.5f;
        private const int SpawnCountPerInterval = 100;

        private bool isInitialized = false;

        private void Start()
        {
            itemPools = new ObjectPool<Item>[itemPrefabs.Length];
            activeItems = new List<Item>[itemPrefabs.Length];
            var maxCapacity = MaxCapacity / itemPrefabs.Length;
            for (var i = 0; i < itemPrefabs.Length; i++)
            {
                var index = i;
                itemPools[i] = new ObjectPool<Item>(() => OnCreate(index), target => OnGet(index, target), target => OnRelease(index, target), OnItemDestroy, defaultCapacity: maxCapacity,
                    maxSize: maxCapacity);
                activeItems[i] = new List<Item>();
            }
            isInitialized = true;
        }

        private void OnDestroy()
        {
            foreach (var pool in itemPools)
                pool.Dispose();
        }

        private void Update()
        {
            if (!isInitialized)
                return;
            var itemCount = activeItems.Sum(items => items.Count);
            activeItemCountLabel.text = $"{itemCount}/{MaxCapacity}";
            elapsed += Time.deltaTime;
            if (elapsed < SpawnInterval)
                return;
            elapsed = 0f;
            for (var count = 0; count < SpawnCountPerInterval; count++)
            {
                var index = Random.Range(0, itemPrefabs.Length);
                try
                {
                    itemPools[index].Get();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private Item OnCreate(int index)
        {
            var pool = itemPools[index];
            var prefab = itemPrefabs[index];
            var instance = Instantiate(prefab, transform);
            instance.Setup(player, () => pool.Release(instance));
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void OnGet(int index, Item target)
        {
            var minBounds = Camera.main.ViewportToWorldPoint(Vector3.zero);
            var maxBounds = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));
            var randomX = Random.Range(minBounds.x, maxBounds.x);
            var randomY = Random.Range(minBounds.y, maxBounds.y);
            target.transform.position = new Vector3(randomX, randomY, 0f);
            target.IsReleased = false;
            target.gameObject.SetActive(true);
            activeItems[index].Add(target);
        }

        private void OnRelease(int index, Item target)
        {
            if (target.IsReleased)
                return;
            target.IsReleased = true;
            target.gameObject.SetActive(false);
            activeItems[index].Remove(target);
        }

        private void OnItemDestroy(Item target)
        {
            Destroy(target);
        }
    }
}
