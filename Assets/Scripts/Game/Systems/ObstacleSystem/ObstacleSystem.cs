using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using Utils.Logger;
using Utils.LogicTimer;
using Utils.Pools;

namespace Game.Systems
{
    public class ObstacleSystem : BaseSystem
    {
        public ObstacleSettings ObstacleSettings { get; private set; }

        private List<ObstacleMover> createdObstacles = new();
        private float timer;

        private const int DefaultPoolCapacity = 25;
        private const int DefaultPoolPreload = 1;
        private Pool obstaclePool;

        public static ObstacleSystem Instance { get; private set; }

        public ObstacleSystem(ObstacleSettings obstacleSettings)
        {
            if (Instance != null && Instance != this)
            {
                Instance.OnDispose();
            }

            Instance = this;

            this.ObstacleSettings = obstacleSettings;

            timer = obstacleSettings.spawnInterval;
        }

        public override void Tick()
        {
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
            CollectableSystem.Instance.SpawnRandomCollectable(collectableSpawnPos);
        }

        private void CreateObstacle(Vector3 position, bool flipVertically)
        {
            var randomIndex = UnityEngine.Random.Range(0, ObstacleSettings.obstaclePrefabs.Count);

            ObstacleMover randomObstaclePrefab = ObstacleSettings.obstaclePrefabs[randomIndex];

            if (obstaclePool == null)
            {
                InitializePool(DefaultPoolPreload, DefaultPoolCapacity, randomObstaclePrefab);
            }

            var obstacleInstance = Pools.Instance.Spawn(randomObstaclePrefab, position, Quaternion.identity, null);
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

        private void InitializePool(int preload, int capacity, ObstacleMover obstaclePrefab)
        {
            if (ObstacleSettings.obstaclePrefabs == null)
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
            base.Dispose();

            foreach (var obstacle in createdObstacles)
            {
                Pools.Instance.Despawn(obstacle.gameObject);
            }

            createdObstacles.Clear();
            createdObstacles = null;
        }
    }   
}