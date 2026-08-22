using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "CloudsConfig", menuName = "Game/Cloud/Clouds Config")]
    public class CloudsConfig : ScriptableObject
    {
        [Header("Visual")]
        public Cloud cloudPrefab;
        public List<Sprite> cloudSprites = new();
        public Color tint = Color.white;
        public int sortingOrder = -10;

        [Header("Spawn Position")]
        public float spawnX = 7f;
        public float spawnZ = 0f;
        public float verticalOriginY = 0f;
        [Min(0f)] public float lowerYOffset = 4f;
        [Min(0f)] public float upperYOffset = 4f;
        public float destroyX = -8f;

        [Header("Generation")]
        [Min(0.01f)] public float minSpawnInterval = 2.5f;
        [Min(0.01f)] public float maxSpawnInterval = 4.5f;
        [Min(1)] public int maxActiveClouds = 6;

        [Header("Movement")]
        [Min(0f)] public float minMoveSpeed = 0.25f;
        [Min(0f)] public float maxMoveSpeed = 0.6f;
        [Min(0.01f)] public float minScale = 0.35f;
        [Min(0.01f)] public float maxScale = 0.65f;

        [Header("Pool")]
        [Min(0)] public int poolPreload = 3;
        [Min(1)] public int poolCapacity = 10;

        private void OnValidate()
        {
            maxSpawnInterval = Mathf.Max(minSpawnInterval, maxSpawnInterval);
            maxMoveSpeed = Mathf.Max(minMoveSpeed, maxMoveSpeed);
            maxScale = Mathf.Max(minScale, maxScale);
            poolCapacity = Mathf.Max(maxActiveClouds, poolCapacity);
        }
    }
}
