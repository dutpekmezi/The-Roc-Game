using NaughtyAttributes;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "CollectableSettings", menuName = "Game/Collectable/Collectable Settings")]
    public class CollectableSettings : ScriptableObject
    {
        public List<Collectable> collectablePrefabs;

        [MaxValue(1), MinValue(0)]
        public float collectableSpawnRate = 0.5f;

        [MaxValue(1), MinValue(0)]
        public float coinSpawnRate = 0.7f;

        public CollectableConfig GetCollectableConfigById(string id)
        {

        }
    }
}