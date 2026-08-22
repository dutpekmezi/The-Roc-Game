using System.Collections.Generic;
using UnityEngine;
using Utils.Logger;
using Utils.LogicTimer;
using Utils.Pools;
using Utils.Signal;

namespace Game.Systems
{
    public class RunObstacleSystem : BaseSystem
    {
        private const int DefaultPoolCapacity = 20;
        private const int DefaultPoolPreload = 3;

        private readonly Transform obstaclesParent;
        private readonly List<RunObstacleMover> activeObstacles = new();
        private readonly List<GameObject> runtimeFallbackPrefabs = new();

        private float spawnTimer;
        private bool movementStopped = true;
        private bool isTopObstacle = false;

        public static RunObstacleSystem Instance { get; private set; }
        public RunObstacleSettings Settings { get; }

        public RunObstacleSystem(RunObstacleSettings settings, Transform obstaclesParent)
        {
            if (Instance != null && Instance != this)
            {
                Instance.Dispose();
            }

            Instance = this;
            Settings = settings != null ? settings : ScriptableObject.CreateInstance<RunObstacleSettings>();
            this.obstaclesParent = obstaclesParent;
            spawnTimer = GetNextSpawnInterval();

            SignalBus.Get<GameplayStartedSignal>().Subscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Subscribe(HandleGameplayStopped);
            WarmUpPools();
        }

        public override void Tick()
        {
            if (movementStopped || Settings == null)
            {
                return;
            }

            spawnTimer -= LogicTimer.FixedDelta;

            if (spawnTimer <= 0f)
            {
                SpawnObstacle();
                spawnTimer = GetNextSpawnInterval();
            }

            for (int i = activeObstacles.Count - 1; i >= 0; i--)
            {
                activeObstacles[i]?.Tick();
            }
        }

        public void ResetForRestart(bool stopMovement = false)
        {
            movementStopped = stopMovement;
            spawnTimer = GetNextSpawnInterval();
            ClearObstacles();
        }

        public void StopMovement()
        {
            movementStopped = true;
        }

        private void SpawnObstacle()
        {

            var random = Random.value;

            isTopObstacle = random <= Settings.groundedSpawnChange ? false : true;
            var targetObstacleList = isTopObstacle ? Settings.TopObstaclePrefabs : Settings.BottombstaclePrefabs;
            var spawnY = isTopObstacle ? Settings.FlyingSpawnY : Settings.GroundedSpawnY;

            var prefab = GetRandomPrefab(targetObstacleList);
            if (prefab == null)
            {
                GameLogger.LogWarning("RunObstacleSystem cannot spawn without an obstacle prefab.");
                return;
            }

            var position = new Vector3(
                Settings.spawnX,
                spawnY,
                Settings.spawnZ);

            var obstacleInstance = Pools.Instance.Spawn(
                prefab,
                position,
                Quaternion.identity,
                obstaclesParent);

            if (obstacleInstance == null)
            {
                GameLogger.LogWarning("RunObstacleSystem could not spawn an obstacle.");
                return;
            }

            obstacleInstance.gameObject.SetActive(true);

            obstacleInstance.Init(this, Settings.moveSpeed, Settings.destroyX);
            activeObstacles.Add(obstacleInstance);
        }

        private float GetNextSpawnInterval()
        {
            return Settings != null ? Settings.GetSpawnInterval() : 0f;
        }

        private float GetSpawnY()
        {

            return isTopObstacle ? Settings.FlyingSpawnY : Settings.GroundedSpawnY;
        }

        private RunObstacleMover GetRandomPrefab(List<RunObstacleMover> obstaclPrefabs)
        {
            if (obstaclPrefabs != null && obstaclPrefabs.Count > 0)
            {
                for (int attempt = 0; attempt < obstaclPrefabs.Count; attempt++)
                {
                    var prefab = obstaclPrefabs[Random.Range(0, obstaclPrefabs.Count)];
                    if (prefab != null)
                    {
                        return prefab;
                    }
                }
            }

            return null;
        }

        private void WarmUpPools()
        {
            if (Pools.Instance == null)
            {
                return;
            }

            if (Settings?.TopObstaclePrefabs == null || Settings.TopObstaclePrefabs.Count == 0)
            {
                var fallbackPrefab = CreateFallbackObstaclePrefab();
                if (fallbackPrefab != null)
                {
                    Pools.Instance.InitializePool(fallbackPrefab, DefaultPoolPreload, DefaultPoolCapacity);
                }

                return;
            }

            if (Settings?.BottombstaclePrefabs == null || Settings.BottombstaclePrefabs.Count == 0)
            {
                var fallbackPrefab = CreateFallbackObstaclePrefab();
                if (fallbackPrefab != null)
                {
                    Pools.Instance.InitializePool(fallbackPrefab, DefaultPoolPreload, DefaultPoolCapacity);
                }

                return;
            }

            int preload = Settings != null ? Settings.poolPreload : DefaultPoolPreload;
            int capacity = Settings != null ? Settings.poolCapacity : DefaultPoolCapacity;

            for (int i = 0; i < Settings.BottombstaclePrefabs.Count; i++)
            {
                var prefab = Settings.BottombstaclePrefabs[i];
                if (prefab != null)
                {
                    Pools.Instance.InitializePool(prefab.gameObject, preload, capacity);
                }
            }

            for (int i = 0; i < Settings.TopObstaclePrefabs.Count; i++)
            {
                var prefab = Settings.TopObstaclePrefabs[i];
                if (prefab != null)
                {
                    Pools.Instance.InitializePool(prefab.gameObject, preload, capacity);
                }
            }
        }

        private GameObject CreateFallbackObstaclePrefab()
        {
            if (runtimeFallbackPrefabs.Count > 0 && runtimeFallbackPrefabs[0] != null)
            {
                return runtimeFallbackPrefabs[0];
            }

            var prefab = new GameObject("RunObstacleFallbackPrefab");
            prefab.SetActive(false);
            var renderer = prefab.AddComponent<SpriteRenderer>();
            renderer.color = new Color(0.22f, 0.22f, 0.22f, 1f);
            renderer.sprite = RuntimeSpriteFactory.WhiteSprite;
            prefab.AddComponent<BoxCollider2D>();
            prefab.AddComponent<RunObstacleMover>();
            prefab.transform.localScale = new Vector3(0.55f, 1.05f, 1f);
            Object.DontDestroyOnLoad(prefab);
            runtimeFallbackPrefabs.Add(prefab);
            return prefab;
        }

        public void DespawnObstacle(RunObstacleMover obstacle)
        {
            if (obstacle == null)
            {
                return;
            }

            activeObstacles.Remove(obstacle);
            Pools.Instance.Despawn(obstacle.gameObject);
        }

        public override void Dispose()
        {
            SignalBus.Get<GameplayStartedSignal>().Unsubscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Unsubscribe(HandleGameplayStopped);

            ClearObstacles();

            for (int i = runtimeFallbackPrefabs.Count - 1; i >= 0; i--)
            {
                if (runtimeFallbackPrefabs[i] != null)
                {
                    Object.Destroy(runtimeFallbackPrefabs[i]);
                }
            }

            runtimeFallbackPrefabs.Clear();

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

        private void ClearObstacles()
        {
            for (int i = activeObstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = activeObstacles[i];
                if (obstacle != null)
                {
                    Pools.Instance.Despawn(obstacle.gameObject);
                }
            }

            activeObstacles.Clear();
        }
    }
}
