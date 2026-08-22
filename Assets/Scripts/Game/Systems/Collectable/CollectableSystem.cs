using Game.Installers;
using GameLift.Audio;
using Game.Systems;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.Logger;
using Utils.LogicTimer;
using Utils.ObjectFlowAnimator;
using Utils.Pools;
using Utils.Signal;

namespace Game.Systems
{
    public class CollectableSystem : BaseSystem
    {
        private const string CollectSoundName = "Collect";
        private const string CollectSpecialSoundName = "Collect_Special";

        public CollectableSettings CollectableSettings { get; private set; }

        private List<Collectable> createdCollectables = new();
        private float timer;
        private bool movementStopped;

        private const int DefaultPoolCapacity = 25;
        private const int DefaultPoolPreload = 1;
        private Pool collectablePool;
        private Pool particlePool;

        private int collectedCollectablesCount;
        private readonly Dictionary<CollectableConfig, int> collectedCounts = new();
        private readonly IAudioService audioService;

        public static CollectableSystem Instance { get; private set; }

        public CollectableSystem(CollectableSettings collectableSettings, IAudioService audioService)
        {
            if (Instance != null && Instance != this)
            {
                Instance.Dispose();
            }

            Instance = this;

            CollectableSettings = collectableSettings;
            this.audioService = audioService;
            movementStopped = true;
            SignalBus.Get<GameplayStartedSignal>().Subscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Subscribe(HandleGameplayStopped);
            WarmUpPools();
        }

        public override void Tick()
        {
            if (createdCollectables == null)
            {
                return;
            }

            if (movementStopped)
            {
                return;
            }

            for (int i = 0; i < createdCollectables.Count; i++)
            {
                if (createdCollectables[i] != null) createdCollectables[i].Tick();
            }
        }

        public void Collect(Collectable collectable)
        {
            var collectableConfig = collectable.CollectableConfig;
            var collectSoundName = collectableConfig != null && collectableConfig.Id == CollectableIds.Coin
                ? CollectSoundName
                : CollectSpecialSoundName;
            audioService?.Play(collectSoundName);

            if (collectableConfig == null)
            {
                GameLogger.LogWarning("CollectableSystem collected item without a collectable config.");
            }
            else
            {
                AddCollectedCount(collectableConfig, 1);
            }

            DespawnCollectable(collectable);
        }

        public void AddCollectedCount(CollectableConfig collectableConfig, int amount)
        {
            if (collectableConfig == null || amount <= 0)
            {
                return;
            }

            var key = GetCollectedCountKey(collectableConfig);
            collectedCounts.TryGetValue(key, out var currentAmount);
            currentAmount += amount;
            collectedCounts[key] = currentAmount;
            collectedCollectablesCount += amount;
            SignalBus.Get<CollectableCollected>().Invoke(key, currentAmount);
        }

        public bool TryGetCollectedCount(CollectableConfig collectableConfig, out int count)
        {
            count = 0;

            if (collectableConfig == null)
            {
                return false;
            }

            if (collectedCounts.TryGetValue(collectableConfig, out count))
            {
                return true;
            }

            if (string.IsNullOrEmpty(collectableConfig.Id))
            {
                return false;
            }

            foreach (var collectedCount in collectedCounts)
            {
                if (collectedCount.Key == null || collectedCount.Key.Id != collectableConfig.Id)
                {
                    continue;
                }

                count += collectedCount.Value;
            }

            return count > 0;
        }

        public IReadOnlyDictionary<CollectableConfig, int> GetCollectedCounts()
        {
            return new Dictionary<CollectableConfig, int>(collectedCounts);
        }

        public void FlyCollectedCollectablesToScreenPosition(CollectableConfig collectableConfig, Vector2 endScreenPos, int count = 1, float startDelay = -1f, Action onReceivedItem = null)
        {
            if (count <= 0)
            {
                return;
            }

            if (startDelay < 0f)
            {
                startDelay = CollectableSettings != null ? CollectableSettings.flyCollectedStartDelay : 0f;
            }

            FlyCollectedCollectables(collectableConfig, () => endScreenPos, count, startDelay, onReceivedItem);
        }

        public void FlyCollectedCollectablesToScreenPosition(CollectableConfig collectableConfig, Func<Vector2> endScreenPosProvider, int count = 1, float startDelay = -1f, Action onReceivedItem = null)
        {
            if (count <= 0)
            {
                return;
            }

            if (startDelay < 0f)
            {
                startDelay = CollectableSettings != null ? CollectableSettings.flyCollectedStartDelay : 0f;
            }

            FlyCollectedCollectables(collectableConfig, endScreenPosProvider, count, startDelay, onReceivedItem);
        }

        private void FlyCollectedCollectables(CollectableConfig collectableConfig, Func<Vector2> endScreenPosProvider, int count = 1, float startDelay = 0f, Action onReceivedItem = null)
        {
            var canvas = GetActiveCanvas();
            var flyDestination = GetCollectableFlyDestination();

            if (CollectableSettings != null && CollectableSettings.collectParticle != null)
            {
                if (particlePool == null)
                {
                    InitializeParticlePool(DefaultPoolPreload, DefaultPoolCapacity, CollectableSettings.collectParticle);
                }

                var spawnPosition = flyDestination != null ? flyDestination.position : Vector3.zero;
                var instance = Pools.Instance.Spawn(
                    CollectableSettings.collectParticle,
                    spawnPosition,
                    Quaternion.identity,
                    GetGameObjectsParent());
                Pools.Instance.Despawn(instance.gameObject, instance.main.duration);
            }


            Vector2 startScreenPos = GetCollectedFlyStartScreenPoint(canvas, flyDestination);

            var parent = GetFlowParent(canvas);
            if (UIFlowAnimator.Instance == null)
            {
                return;
            }

            Func<Vector3> endScreenPosProvider3d = null;
            if (endScreenPosProvider != null)
            {
                endScreenPosProvider3d = () => endScreenPosProvider();
            }

            UIFlowAnimator.Instance.AddNewDestinationAction(
                startScreenPos: startScreenPos,
                endScreenPosProvider: endScreenPosProvider3d,
                sprite: collectableConfig != null ? collectableConfig.Icon : null,
                parent: parent,
                particleCount: count,
                startDelay: startDelay,
                onReceivedItem: onReceivedItem
                // TEMP: Fly arrival sounds are disabled.
                // receivedSoundName: collectableConfig != null && collectableConfig.Id == CollectableIds.Coin
                //     ? "Fly_Gold"
                //     : "Fly_Collectable"
            );
        }

        private CollectableConfig GetCollectedCountKey(CollectableConfig collectableConfig)
        {
            if (collectableConfig == null || string.IsNullOrEmpty(collectableConfig.Id))
            {
                return collectableConfig;
            }

            foreach (var collectedCount in collectedCounts)
            {
                var existingConfig = collectedCount.Key;
                if (existingConfig != null && existingConfig.Id == collectableConfig.Id)
                {
                    return existingConfig;
                }
            }

            return collectableConfig;
        }

        public void TrySpawnRandomCollectable(Vector2 spawnPos)
        {
            if (CollectableSettings == null)
            {
                return;
            }

            var random = UnityEngine.Random.Range(0f, 1f);
            if (random < CollectableSettings.collectableSpawnRate)
            {
                CreateCollectable(spawnPos);
            }
        }

        [System.Obsolete("Use TrySpawnRandomCollectable instead.")]
        public void SpawnRandomCollectable(Vector2 spawnPos)
        {
            TrySpawnRandomCollectable(spawnPos);
        }

        private void CreateCollectable(Vector3 position, Collectable collectablePrefab = null)
        {
            if (collectablePrefab == null)
            {
                var random = UnityEngine.Random.Range(0f, 1f);
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
                InitializeColletablePool(DefaultPoolPreload, DefaultPoolCapacity, collectablePrefab);
            }

            var collectableInstance = Pools.Instance.Spawn(collectablePrefab, position, Quaternion.identity, GameInstaller.Instance.GameObjectsParent);
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

        private void InitializeColletablePool(int preload, int capacity, Collectable collectablePrefab)
        {
            if (collectablePrefab == null)
            {
                GameLogger.LogWarning("CollectableSystem cannot initialize pool without a collectable prefab.");
                return;
            }

            if (CollectableSettings == null || CollectableSettings.collectablePrefabs == null)
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

        private void InitializeParticlePool(int preload, int capacity, ParticleSystem particle)
        {
            if (particle == null)
            {
                GameLogger.LogWarning("CollectableSystem cannot initialize pool without a collect particle prefab.");
                return;
            }

            if (capacity > 0)
            {
                particlePool = Pools.Instance.InitializePool(particle.gameObject, preload, capacity);
            }
            else
            {
                particlePool = Pools.Instance.InitializePool(particle.gameObject, preload);
            }
        }

        public void DespawnCollectable(Collectable collectable)
        {
            Pools.Instance.Despawn(collectable.gameObject);
            createdCollectables.Remove(collectable);
        }

        public void ResetForRestart(bool stopMovement = false)
        {
            movementStopped = stopMovement;
            ClearCreatedCollectables();
            createdCollectables ??= new List<Collectable>();
            ResetCollectedCounts();
        }

        public void StopMovement()
        {
            movementStopped = true;
        }

        private void WarmUpPools()
        {
            if (CollectableSettings == null)
            {
                return;
            }

            if (CollectableSettings.collectablePrefabs != null)
            {
                for (int i = 0; i < CollectableSettings.collectablePrefabs.Count; i++)
                {
                    var collectablePrefab = CollectableSettings.collectablePrefabs[i];
                    if (collectablePrefab == null)
                    {
                        continue;
                    }

                    var pool = Pools.Instance.InitializePool(
                        collectablePrefab.gameObject,
                        DefaultPoolPreload,
                        DefaultPoolCapacity);

                    collectablePool ??= pool;
                }
            }

            if (CollectableSettings.collectParticle != null)
            {
                particlePool = Pools.Instance.InitializePool(
                    CollectableSettings.collectParticle.gameObject,
                    DefaultPoolPreload,
                    DefaultPoolCapacity);
            }
        }

        private static Vector2 GetScreenPoint(Canvas canvas, RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return Vector2.zero;
            }

            Camera camera = null;

            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }

            return RectTransformUtility.WorldToScreenPoint(camera, rectTransform.position);
        }

        private static Canvas GetActiveCanvas()
        {
            if (GameInstaller.Instance != null && GameInstaller.Instance.Canvas != null)
            {
                return GameInstaller.Instance.Canvas;
            }

            if (RunGameInstaller.Instance != null && RunGameInstaller.Instance.Canvas != null)
            {
                return RunGameInstaller.Instance.Canvas;
            }

            return CoreInstaller.Instance != null ? CoreInstaller.Instance.Canvas : null;
        }

        private static RectTransform GetCollectableFlyDestination()
        {
            if (GameInstaller.Instance != null && GameInstaller.Instance.CollectableFlyDestination != null)
            {
                return GameInstaller.Instance.CollectableFlyDestination;
            }

            if (RunGameInstaller.Instance != null && RunGameInstaller.Instance.CollectableFlyDestination != null)
            {
                return RunGameInstaller.Instance.CollectableFlyDestination;
            }

            return null;
        }

        private static Transform GetGameObjectsParent()
        {
            if (GameInstaller.Instance != null && GameInstaller.Instance.GameObjectsParent != null)
            {
                return GameInstaller.Instance.GameObjectsParent;
            }

            if (RunGameInstaller.Instance != null && RunGameInstaller.Instance.GameObjectsParent != null)
            {
                return RunGameInstaller.Instance.GameObjectsParent;
            }

            return CoreInstaller.Instance != null ? CoreInstaller.Instance.transform : null;
        }

        private static Vector2 GetCollectedFlyStartScreenPoint(Canvas canvas, RectTransform flyDestination)
        {
            if (flyDestination != null)
            {
                return GetScreenPoint(canvas, flyDestination);
            }

            var playerTransform = PlayerSystem.Instance?.GetPlayerTransform();
            if (playerTransform != null && Camera.main != null)
            {
                return Camera.main.WorldToScreenPoint(playerTransform.position);
            }

            var runnerTransform = RunPlayerSystem.Instance?.GetPlayerTransform();
            if (runnerTransform != null && Camera.main != null)
            {
                return Camera.main.WorldToScreenPoint(runnerTransform.position);
            }

            return Vector2.zero;
        }

        private static RectTransform GetFlowParent(Canvas canvas)
        {
            if (CoreInstaller.Instance != null && CoreInstaller.Instance.Canvas != null)
            {
                return CoreInstaller.Instance.Canvas.transform as RectTransform;
            }

            return canvas != null ? canvas.transform as RectTransform : null;
        }

        public override void Dispose()
        {
            SignalBus.Get<GameplayStartedSignal>().Unsubscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Unsubscribe(HandleGameplayStopped);
            ClearCreatedCollectables();
            createdCollectables = null;
            collectedCounts.Clear();
            collectedCollectablesCount = 0;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void HandleGameplayStarted()
        {
            ResetForRestart();
        }

        private void HandleGameplayStopped()
        {
            StopMovement();
        }

        private void ClearCreatedCollectables()
        {
            if (createdCollectables != null)
            {
                for (int i = createdCollectables.Count - 1; i >= 0; i--)
                {
                    var collectable = createdCollectables[i];
                    if (collectable != null) Pools.Instance.Despawn(collectable.gameObject);
                }

                createdCollectables.Clear();
            }
        }

        private void ResetCollectedCounts()
        {
            if (collectedCounts.Count > 0)
            {
                foreach (var collectedCount in collectedCounts)
                {
                    if (collectedCount.Key != null)
                    {
                        SignalBus.Get<CollectableCollected>().Invoke(collectedCount.Key, 0);
                    }
                }

                collectedCounts.Clear();
            }

            collectedCollectablesCount = 0;
        }

        public class CollectableCollected : Signal<CollectableConfig, int> { }
    }
}
