using System.Collections.Generic;
using UnityEngine;
using Utils.Logger;
using Utils.LogicTimer;
using Utils.Pools;

namespace Game.Systems
{
    public class CloudGeneratorSystem : BaseSystem
    {
        private readonly Transform cloudsParent;
        private readonly List<Cloud> activeClouds = new();

        private Pool cloudPool;
        private float spawnTimer;

        public CloudsConfig CloudsConfig { get; }

        public CloudGeneratorSystem(CloudsConfig cloudsConfig, Transform cloudsParent)
        {
            CloudsConfig = cloudsConfig;
            this.cloudsParent = cloudsParent;
            spawnTimer = 0f;
        }

        public override void Tick()
        {
            for (int i = activeClouds.Count - 1; i >= 0; i--)
            {
                activeClouds[i]?.Tick();
            }

            if (!CanGenerateClouds())
            {
                return;
            }

            spawnTimer -= LogicTimer.FixedDelta;

            if (spawnTimer <= 0f)
            {
                if (activeClouds.Count < CloudsConfig.maxActiveClouds)
                {
                    SpawnCloud();
                }

                spawnTimer = Random.Range(
                    CloudsConfig.minSpawnInterval,
                    CloudsConfig.maxSpawnInterval);
            }
        }

        private bool CanGenerateClouds()
        {
            return CloudsConfig != null
                && CloudsConfig.cloudPrefab != null
                && CloudsConfig.cloudSprites != null
                && CloudsConfig.cloudSprites.Count > 0
                && CloudsConfig.maxActiveClouds > 0;
        }

        private void SpawnCloud()
        {
            EnsurePool();

            float spawnY = CloudsConfig.verticalOriginY + Random.Range(
                -CloudsConfig.lowerYOffset,
                CloudsConfig.upperYOffset);
            var spawnPosition = new Vector3(
                CloudsConfig.spawnX,
                spawnY,
                CloudsConfig.spawnZ);

            var cloud = Pools.Instance.Spawn(
                CloudsConfig.cloudPrefab,
                spawnPosition,
                Quaternion.identity,
                cloudsParent);

            if (cloud == null)
            {
                GameLogger.LogWarning("CloudGeneratorSystem could not spawn a cloud.");
                return;
            }

            var sprite = CloudsConfig.cloudSprites[
                Random.Range(0, CloudsConfig.cloudSprites.Count)];
            float speed = Random.Range(
                CloudsConfig.minMoveSpeed,
                CloudsConfig.maxMoveSpeed);
            float scale = Random.Range(
                CloudsConfig.minScale,
                CloudsConfig.maxScale);

            cloud.Init(
                this,
                sprite,
                speed,
                scale,
                CloudsConfig.sortingOrder,
                CloudsConfig.tint);

            activeClouds.Add(cloud);
        }

        private void EnsurePool()
        {
            if (cloudPool != null)
            {
                return;
            }

            cloudPool = Pools.Instance.InitializePool(
                CloudsConfig.cloudPrefab.gameObject,
                CloudsConfig.poolPreload,
                CloudsConfig.poolCapacity);
        }

        public void DespawnCloud(Cloud cloud)
        {
            if (cloud == null)
            {
                return;
            }

            activeClouds.Remove(cloud);
            Pools.Instance.Despawn(cloud);
        }

        public override void Dispose()
        {
            for (int i = activeClouds.Count - 1; i >= 0; i--)
            {
                var cloud = activeClouds[i];
                if (cloud != null)
                {
                    Pools.Instance.Despawn(cloud);
                }
            }

            activeClouds.Clear();
        }
    }
}
