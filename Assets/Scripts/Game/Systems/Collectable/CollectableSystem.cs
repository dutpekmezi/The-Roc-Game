using Game.Systems;
using System.Collections.Generic;
using UnityEngine;
using Utils.Currency;
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
        private int collectedCoffeeCount;
        private int collectedMatchaCount;
        private int collectedCoinCount;

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

            if (collectable.CurrencyConfig.currencyId == CurrencyIds.Matcha)
            {
                collectedMatchaCount++;
            }
            else if (collectable.CurrencyConfig.currencyId == CurrencyIds.Coffee)
            {
                collectedCoffeeCount++;
            }
            else if (collectable.CurrencyConfig.currencyId == CurrencyIds.Coin)
            {
                collectedCoinCount++;
            }

            var currencId = collectable.CurrencyConfig.currencyId;

            SignalBus.Get<CollectableCollected>().Invoke(currencId, 
                currencId == CurrencyIds.Matcha ? collectedMatchaCount : 
                currencId == CurrencyIds.Coffee ? collectedCoffeeCount :
                currencId == CurrencyIds.Coin ? collectedCoinCount : 0);
            DespawnCollectable(collectable);
        }

        public void SpawnRandomCollectable(Vector2 spawnPos)
        {
            var randomIndex = Random.Range(0, CollectableSettings.collectablePrefabs.Count);
            var randomCollectablePrefab = CollectableSettings.collectablePrefabs[randomIndex];
            CreateCollectable(spawnPos, randomCollectablePrefab);
        }

        private void CreateCollectable(Vector3 position, Collectable collectablePrefab = null)
        {
            
            if (collectablePrefab == null)
            {
                var randomIndex = UnityEngine.Random.Range(0, CollectableSettings.collectablePrefabs.Count);
                collectablePrefab = CollectableSettings.collectablePrefabs[randomIndex];
            }

            if (collectablePool == null)
            {
                InitializePool(DefaultPoolPreload, DefaultPoolCapacity, collectablePrefab);
            }

            var collectableInstance = Pools.Instance.Spawn(collectablePrefab, position, Quaternion.identity, null);
            collectableInstance.Init(this);
            createdCollectables.Add(collectableInstance);
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

        public class CollectableCollected : Signal<string, int> { }
    }
}