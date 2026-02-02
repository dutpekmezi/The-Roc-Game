using Game.Systems;
using System.Collections.Generic;
using UnityEngine;
using Utils.Logger;
using Utils.LogicTimer;
using Utils.Pools;
using Utils.Signal;

namespace Game.Systems
{
    public class CollectableSystem : BaseSystem
    {
        public CollectableSettings CollectableSettings { get; private set; }

        private List<Collectable> createdCollectables = new();
        private float timer;

        private const int DefaultPoolCapacity = 25;
        private const int DefaultPoolPreload = 1;
        private Pool collectablePool;

        private int collectedCollectablesCount;
        private readonly Dictionary<CollectableConfig, int> collectedCounts = new();

        public static CollectableSystem Instance { get; private set; }

        public CollectableSystem(CollectableSettings collectableSettings)
        {
            if (Instance != null && Instance != this)
            {
                Instance.OnDispose();
            }

            Instance = this;

            CollectableSettings = collectableSettings;
        }

        public override void Tick()
        {
            for (int i = 0; i < createdCollectables.Count; i++)
            {
                if (createdCollectables[i] != null) createdCollectables[i].Tick();
            }
        }

        public void Collect(Collectable collectable)
        {
            collectedCollectablesCount++;

            var collectableConfig = collectable.CollectableConfig;
            if (collectableConfig == null)
            {
                GameLogger.LogWarning("CollectableSystem collected item without a collectable config.");
            }
            else
            {
                collectedCounts.TryGetValue(collectableConfig, out var currentAmount);
                currentAmount++;
                collectedCounts[collectableConfig] = currentAmount;
                SignalBus.Get<CollectableCollected>().Invoke(collectableConfig, currentAmount);
            }

            DespawnCollectable(collectable);
        }

        [System.Obsolete]
        public void SpawnRandomCollectable(Vector2 spawnPos)
        {
            var random = Random.RandomRange(0f, 1f);
            if (random < CollectableSettings.collectableSpawnRate)
            {
                CreateCollectable(spawnPos);
            }
        }

        [System.Obsolete]
        private void CreateCollectable(Vector3 position, Collectable collectablePrefab = null)
        {
            if (collectablePrefab == null)
            {
                var random = Random.RandomRange(0f, 1f);
                if (random < CollectableSettings.coinSpawnRate)
                {
                    var coinConfig = CollectableSettings.GetCollectableConfigById(CollectableIds.Coin);
                    collectablePrefab = GetCollectablePrefabByConfig(coinConfig);
                }
                else
                {
                    collectablePrefab = GetRandomNonCoinCollectablePrefab();
                }

                if (collectablePrefab == null && CollectableSettings.collectablePrefabs != null && CollectableSettings.collectablePrefabs.Count > 0)
                {
                    var randomIndex = UnityEngine.Random.Range(0, CollectableSettings.collectablePrefabs.Count);
                    collectablePrefab = CollectableSettings.collectablePrefabs[randomIndex];
                }
            }

            if (collectablePool == null)
            {
                InitializePool(DefaultPoolPreload, DefaultPoolCapacity, collectablePrefab);
            }

            var collectableInstance = Pools.Instance.Spawn(collectablePrefab, position, Quaternion.identity, null);
            collectableInstance.Init(this);
            createdCollectables.Add(collectableInstance);
        }

        private Collectable GetRandomNonCoinCollectablePrefab()
        {
            if (CollectableSettings.collectablePrefabs == null || CollectableSettings.collectablePrefabs.Count == 0)
            {
                return null;
            }

            var nonCoinPrefabs = new List<Collectable>();
            for (int i = 0; i < CollectableSettings.collectablePrefabs.Count; i++)
            {
                var prefab = CollectableSettings.collectablePrefabs[i];
                if (prefab == null)
                {
                    continue;
                }

                var config = prefab.CollectableConfig;
                if (config != null && config.Id != CollectableIds.Coin)
                {
                    nonCoinPrefabs.Add(prefab);
                }
            }

            if (nonCoinPrefabs.Count == 0)
            {
                return null;
            }

            var randomIndex = UnityEngine.Random.Range(0, nonCoinPrefabs.Count);
            return nonCoinPrefabs[randomIndex];
        }

        private Collectable GetCollectablePrefabByConfig(CollectableConfig config)
        {
            if (config == null || CollectableSettings.collectablePrefabs == null)
            {
                return null;
            }

            for (int i = 0; i < CollectableSettings.collectablePrefabs.Count; i++)
            {
                var prefab = CollectableSettings.collectablePrefabs[i];
                if (prefab != null && prefab.CollectableConfig == config)
                {
                    return prefab;
                }
            }

            return null;
        }

        private void InitializePool(int preload, int capacity, Collectable collectablePrefab)
        {
            if (CollectableSettings.collectablePrefabs == null)
            {
                GameLogger.LogWarning("CollectableSystem cannot initialize pool without a collectable prefab.");
                return;
            }

            if (capacity > 0)
            {
                collectablePool = Pools.Instance.InitializePool(collectablePrefab.gameObject, preload, capacity);
            }
            else
            {
                collectablePool = Pools.Instance.InitializePool(collectablePrefab.gameObject, preload);
            }
        }

        public void DespawnCollectable(Collectable collectable)
        {
            Pools.Instance.Despawn(collectable.gameObject);
            createdCollectables.Remove(collectable);
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (Collectable collectable in createdCollectables)
            {
                Pools.Instance.Despawn(collectable.gameObject);
            }

            createdCollectables.Clear();
            collectedCounts.Clear();

            createdCollectables = null;
            
            collectedCollectablesCount = 0;
        }

        public class CollectableCollected : Signal<CollectableConfig, int> { }
    }
}
