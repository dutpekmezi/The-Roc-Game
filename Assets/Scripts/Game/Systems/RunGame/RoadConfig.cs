using UnityEngine;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "RoadConfig", menuName = "Game/Run/Road Config")]
    public class RoadConfig : ScriptableObject
    {
        [Header("Prefabs")]
        public GameObject firstRoadPrefab;
        public GameObject secondRoadPrefab;

        [Header("Movement")]
        [Min(0f)] public float moveSpeed = 4f;

        [Header("Layout")]
        public float startX = 0f;
        public float startY = -2.96f;
        public float startZ = 0f;
        [Min(0.01f)] public float tileWidth = 10.24f;
        [Min(0f)] public float recyclePadding = 0.05f;

        [Header("Generation")]
        [Min(0f)] public float minSpawnInterval = 0.2f;
        [Min(0f)] public float maxSpawnInterval = 0.6f;

        public GameObject GetPrefab(int index)
        {
            return index == 0 ? firstRoadPrefab : secondRoadPrefab;
        }

        public float GetSpawnInterval()
        {
            float minInterval = Mathf.Max(0f, minSpawnInterval);
            float maxInterval = Mathf.Max(minInterval, maxSpawnInterval);
            return Random.Range(minInterval, maxInterval);
        }

        private void OnValidate()
        {
            tileWidth = Mathf.Max(0.01f, tileWidth);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            recyclePadding = Mathf.Max(0f, recyclePadding);
            minSpawnInterval = Mathf.Max(0f, minSpawnInterval);
            maxSpawnInterval = Mathf.Max(minSpawnInterval, maxSpawnInterval);
        }
    }
}
