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

        [MinValue(0)]
        public float flyGoldStartDelay = 0f;

        [MinValue(0)]
        public float flyCollectedStartDelay = 0f;

        public List<CollectableConfig> GetCollectableConfigs()
        {
            var configs = new List<CollectableConfig>();
            if (collectablePrefabs == null)
            {
                return configs;
            }

            for (int i = 0; i < collectablePrefabs.Count; i++)
            {
                var collectablePrefab = collectablePrefabs[i];
                if (collectablePrefab == null || collectablePrefab.CollectableConfig == null)
                {
                    continue;
                }

                var config = collectablePrefab.CollectableConfig;
                if (!ContainsCollectableConfig(configs, config))
                {
                    configs.Add(config);
                }
            }

            return configs;
        }

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

        private static bool ContainsCollectableConfig(List<CollectableConfig> configs, CollectableConfig config)
        {
            if (configs == null || config == null)
            {
                return false;
            }

            for (int i = 0; i < configs.Count; i++)
            {
                var existingConfig = configs[i];
                if (existingConfig == null)
                {
                    continue;
                }

                if (existingConfig == config || (!string.IsNullOrEmpty(existingConfig.Id) && existingConfig.Id == config.Id))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
