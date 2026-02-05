using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using Utils.ObjectFlowAnimator;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "CollectableSettings", menuName = "Game/Collectable/Collectable Settings")]
    public class CollectableSettings : ScriptableObject
    {
        public List<Collectable> collectablePrefabs;
        public ParticleSystem collectParticle;

        [MaxValue(1), MinValue(0)]
        public float collectableSpawnRate = 0.5f;

        [MaxValue(1), MinValue(0)]
        public float coinSpawnRate = 0.7f;

        public CollectableConfig GetCollectableConfigById(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || collectablePrefabs == null)
            {
                return null;
            }

            for (int i = 0; i < collectablePrefabs.Count; i++)
            {
                var collectablePrefab = collectablePrefabs[i];
                if (collectablePrefab == null)
                {
                    continue;
                }

                var config = collectablePrefab.CollectableConfig;
                if (config != null && config.Id == id)
                {
                    return config;
                }
            }

            return null;
        }
    }
}
