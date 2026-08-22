using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "RunObstacleSettings", menuName = "Game/Run/Run Obstacle Settings")]
    public class RunObstacleSettings : ScriptableObject
    {
        [Header("Movement")]
        [Min(0f)] public float moveSpeed = 4f;
        public float destroyX = -9f;

        [Header("Spawn")]
        public float groundedSpawnChange;

        [FormerlySerializedAs("spawnInterval")]
        [Min(0.05f)] public float minSpawnInterval = 1.25f;
        [Min(0.05f)] public float maxSpawnInterval = 1.75f;
        public float spawnX = 8f;
        public float spawnZ = 0f;
        public float GroundedSpawnY = -3.25f;
        public float FlyingSpawnY = -1.75f;

        [Header("Pool")]
        [Min(0)] public int poolPreload = 3;
        [Min(1)] public int poolCapacity = 20;

        [Header("Prefabs")]
        public List<RunObstacleMover> BottombstaclePrefabs = new();
        public List<RunObstacleMover> TopObstaclePrefabs = new();

        private void OnValidate()
        {
            minSpawnInterval = Mathf.Max(0.05f, minSpawnInterval);
            maxSpawnInterval = Mathf.Max(minSpawnInterval, maxSpawnInterval);
            poolCapacity = Mathf.Max(1, poolCapacity);
            poolPreload = Mathf.Clamp(poolPreload, 0, poolCapacity);
        }

        public float GetSpawnInterval()
        {
            float minInterval = Mathf.Max(0.05f, minSpawnInterval);
            float maxInterval = Mathf.Max(minInterval, maxSpawnInterval);
            return Random.Range(minInterval, maxInterval);
        }
    }
}
