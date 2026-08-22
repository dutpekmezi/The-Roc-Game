using Game.Installers;
using System.Collections.Generic;
using UnityEngine;
using Utils.Logger;
using Utils.LogicTimer;
using Utils.Pools;
using Utils.Signal;

namespace Game.Systems
{
    public class ObstacleSystem : BaseSystem
    {
        public ObstacleSettings ObstacleSettings { get; private set; }

        private List<ObstacleMover> createdObstacles = new();
        private float timer;
        private bool movementStopped;

        private const int DefaultPoolCapacity = 25;
        private const int DefaultPoolPreload = 1;
        private Pool obstaclePool;

        public static ObstacleSystem Instance { get; private set; }

        public ObstacleSystem(ObstacleSettings obstacleSettings)
        {
            if (Instance != null && Instance != this)
            {
                Instance.Dispose();
            }

            Instance = this;

            ObstacleSettings = obstacleSettings;
            movementStopped = true;

            timer = ObstacleSettings != null ? ObstacleSettings.spawnInterval : 0f;
            SignalBus.Get<GameplayStartedSignal>().Subscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Subscribe(HandleGameplayStopped);
            WarmUpPools();
        }

        public override void Tick()
        {
            if (createdObstacles == null || ObstacleSettings == null)
            {
                return;
            }

            if (movementStopped)
            {
                return;
            }

            timer -= LogicTimer.FixedDelta;

            if (timer <= 0)
            {
                SpawnObstacle();
                timer = ObstacleSettings.spawnInterval;
            }

            for (int i = 0; i < createdObstacles.Count; i++)
            {
                if (createdObstacles[i] != null) createdObstacles[i].Tick();
            }
        }

        private void SpawnObstacle()
        {
            float spawnY = UnityEngine.Random.Range(ObstacleSettings.minY, ObstacleSettings.maxY);
            Vector3 spawnPosition = new Vector2(ObstacleSettings.spawnX, spawnY);
            float gap = UnityEngine.Random.Range(ObstacleSettings.minGap, ObstacleSettings.maxGap);

            var targetSpawnPosYBottom = spawnPosition.y;
            var targetSpawnPosYTop = ((Vector2)spawnPosition + Vector2.up * (gap)).y;

            

            if (targetSpawnPosYTop > ObstacleSettings.maxY)
            {
                var diff = targetSpawnPosYTop - ObstacleSettings.maxY;
                targetSpawnPosYTop -= diff;
                targetSpawnPosYBottom -= diff;
            }
            else if (targetSpawnPosYBottom < ObstacleSettings.minY)
            {
                var diff = targetSpawnPosYBottom - ObstacleSettings.minY;
                targetSpawnPosYBottom -= diff;
                targetSpawnPosYTop -= diff;
            }

            var collectableSpawnPos = new Vector2(ObstacleSettings.spawnX, targetSpawnPosYBottom + ((targetSpawnPosYTop - targetSpawnPosYBottom) * 0.5f));

            CreateObstacle(new Vector2(ObstacleSettings.spawnX, targetSpawnPosYBottom), false);
            CreateObstacle(new Vector2(ObstacleSettings.spawnX, targetSpawnPosYTop), true);
            CollectableSystem.Instance.TrySpawnRandomCollectable(collectableSpawnPos);
        }

        private void CreateObstacle(Vector3 position, bool flipVertically)
        {
            var randomIndex = UnityEngine.Random.Range(0, ObstacleSettings.obstaclePrefabs.Count);

            ObstacleMover randomObstaclePrefab = ObstacleSettings.obstaclePrefabs[randomIndex];

            if (obstaclePool == null)
            {
                InitializePool(DefaultPoolPreload, DefaultPoolCapacity, randomObstaclePrefab);
            }

            var obstacleInstance = Pools.Instance.Spawn(randomObstaclePrefab, position, Quaternion.identity, GameInstaller.Instance.GameObjectsParent);
            obstacleInstance.Init(this);
            createdObstacles.Add(obstacleInstance);

            if (flipVertically)
            {
                obstacleInstance.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            }
            else
            {
                obstacleInstance.transform.rotation = Quaternion.identity;
            }
        }

        public void DespawnObstacle(ObstacleMover obstacle)
        {
            Pools.Instance.Despawn(obstacle.gameObject);
            createdObstacles.Remove(obstacle);
        }

        public void ResetForRestart(bool stopMovement = false)
        {
            movementStopped = stopMovement;
            ClearCreatedObstacles();
            createdObstacles ??= new List<ObstacleMover>();
            timer = ObstacleSettings != null ? ObstacleSettings.spawnInterval : 0f;
        }

        public void StopMovement()
        {
            movementStopped = true;
        }

        private void WarmUpPools()
        {
            if (ObstacleSettings == null || ObstacleSettings.obstaclePrefabs == null)
            {
                GameLogger.LogWarning("ObstacleSystem cannot initialize pool without a obstacle prefab.");
                return;
            }

            for (int i = 0; i < ObstacleSettings.obstaclePrefabs.Count; i++)
            {
                var obstaclePrefab = ObstacleSettings.obstaclePrefabs[i];
                if (obstaclePrefab == null)
                {
                    continue;
                }

                var pool = Pools.Instance.InitializePool(
                    obstaclePrefab.gameObject,
                    DefaultPoolPreload,
                    DefaultPoolCapacity);

                obstaclePool ??= pool;
            }
        }

        private void InitializePool(int preload, int capacity, ObstacleMover obstaclePrefab)
        {
            if (obstaclePrefab == null)
            {
                GameLogger.LogWarning("ObstacleSystem cannot initialize pool without a obstacle prefab.");
                return;
            }

            if (ObstacleSettings == null || ObstacleSettings.obstaclePrefabs == null)
            {
                GameLogger.LogWarning("ObstacleSystem cannot initialize pool without a obstacle prefab.");
                return;
            }

            if (capacity > 0)
            {
                obstaclePool = Pools.Instance.InitializePool(obstaclePrefab.gameObject, preload, capacity);
            }
            else
            {
                obstaclePool = Pools.Instance.InitializePool(obstaclePrefab.gameObject, preload);
            }
        }

        public override void Dispose()
        {
            SignalBus.Get<GameplayStartedSignal>().Unsubscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Unsubscribe(HandleGameplayStopped);
            ClearCreatedObstacles();
            createdObstacles = null;

            return;
        }

        private void HandleGameplayStarted()
        {
            ResetForRestart();
        }

        private void HandleGameplayStopped()
        {
            StopMovement();
        }

        private void ClearCreatedObstacles()
        {
            if (createdObstacles == null)
            {
                return;
            }

            for (int i = createdObstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = createdObstacles[i];
                if (obstacle != null) Pools.Instance.Despawn(obstacle.gameObject);
            }

            createdObstacles.Clear();
        }
    }   
}
