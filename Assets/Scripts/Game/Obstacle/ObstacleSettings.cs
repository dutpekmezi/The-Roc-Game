using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "ObstacleSettings", menuName = "Game/Obstacle/Obstacle Settings")]
    public class ObstacleSettings : ScriptableObject
    {
        public float moveSpeed = 2f;
        public float destroyX = -12f;
        public float spawnInterval = 1.75f;
        public float spawnX = 9f;
        public float minY = -1.5f;
        public float maxY = 2.5f;
        public float minGap = 2.5f;
        public float maxGap = 4f;
        public List<ObstacleMover> obstaclePrefabs;
    }
}