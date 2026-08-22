using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems
{
    public enum ScoreType
    {
        Default = 0,
        PlayTime = 1
    }

    [Serializable]
    public class ScoreCollectableRewardConfig
    {
        public CollectableConfig collectableConfig;
        [Min(0f)] public float scoreRewardRatio = 0f;

        public int CalculateRewardAmount(int totalScore)
        {
            if (collectableConfig == null || totalScore <= 0 || scoreRewardRatio <= 0f)
            {
                return 0;
            }

            return Mathf.FloorToInt(totalScore * scoreRewardRatio);
        }
    }

    [CreateAssetMenu(fileName = "ScoreConfig", menuName = "Game/Run/Score Config")]
    public class ScoreConfig : ScriptableObject
    {
        [Header("Mode")]
        public ScoreType scoreType = ScoreType.Default;

        [Header("Score")]
        [Min(0)] public int initialScore = 0;
        [Min(0f)] public float scorePerSecond = 10f;
        [Min(1)] public int defaultGainAmount = 1;

        [Header("Score Trigger")]
        [Min(1)] public int scoreTrigging = 100;
        public string scoreTriggerSoundName = "Score_Sound";

        [Header("Collectable Rewards")]
        [SerializeField] private CollectableSettings collectableSettings;
        [SerializeField] private List<ScoreCollectableRewardConfig> collectableRewardConfigs = new();

        public CollectableSettings CollectableSettings => collectableSettings;
        public IReadOnlyList<ScoreCollectableRewardConfig> CollectableRewardConfigs => collectableRewardConfigs;

        public Dictionary<CollectableConfig, int> CalculateCollectableRewards(int totalScore)
        {
            SyncCollectableRewardConfigs();

            var rewards = new Dictionary<CollectableConfig, int>();
            if (totalScore <= 0 || collectableRewardConfigs == null)
            {
                return rewards;
            }

            for (int i = 0; i < collectableRewardConfigs.Count; i++)
            {
                var rewardConfig = collectableRewardConfigs[i];
                if (rewardConfig == null || rewardConfig.collectableConfig == null)
                {
                    continue;
                }

                int amount = rewardConfig.CalculateRewardAmount(totalScore);
                if (amount <= 0)
                {
                    continue;
                }

                var key = GetRewardKey(rewards, rewardConfig.collectableConfig);
                rewards.TryGetValue(key, out int currentAmount);
                rewards[key] = currentAmount + amount;
            }

            return rewards;
        }

        public void SyncCollectableRewardConfigs()
        {
            collectableRewardConfigs ??= new List<ScoreCollectableRewardConfig>();

            for (int i = 0; i < collectableRewardConfigs.Count; i++)
            {
                if (collectableRewardConfigs[i] == null)
                {
                    collectableRewardConfigs[i] = new ScoreCollectableRewardConfig();
                }

                collectableRewardConfigs[i].scoreRewardRatio = Mathf.Max(0f, collectableRewardConfigs[i].scoreRewardRatio);
            }

            if (collectableSettings == null)
            {
                return;
            }

            var collectableConfigs = collectableSettings.GetCollectableConfigs();
            for (int i = 0; i < collectableConfigs.Count; i++)
            {
                var collectableConfig = collectableConfigs[i];
                if (collectableConfig == null || HasRewardConfig(collectableConfig))
                {
                    continue;
                }

                collectableRewardConfigs.Add(new ScoreCollectableRewardConfig
                {
                    collectableConfig = collectableConfig,
                    scoreRewardRatio = 0f
                });
            }
        }

        private void OnValidate()
        {
            initialScore = Mathf.Max(0, initialScore);
            scorePerSecond = Mathf.Max(0f, scorePerSecond);
            defaultGainAmount = Mathf.Max(1, defaultGainAmount);
            scoreTrigging = Mathf.Max(1, scoreTrigging);
            SyncCollectableRewardConfigs();
        }

        private bool HasRewardConfig(CollectableConfig collectableConfig)
        {
            for (int i = 0; i < collectableRewardConfigs.Count; i++)
            {
                var configuredCollectable = collectableRewardConfigs[i]?.collectableConfig;
                if (IsSameCollectable(configuredCollectable, collectableConfig))
                {
                    return true;
                }
            }

            return false;
        }

        private static CollectableConfig GetRewardKey(
            Dictionary<CollectableConfig, int> rewards,
            CollectableConfig collectableConfig)
        {
            if (string.IsNullOrEmpty(collectableConfig.Id))
            {
                return collectableConfig;
            }

            foreach (var reward in rewards)
            {
                if (IsSameCollectable(reward.Key, collectableConfig))
                {
                    return reward.Key;
                }
            }

            return collectableConfig;
        }

        private static bool IsSameCollectable(CollectableConfig left, CollectableConfig right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left == right || (!string.IsNullOrEmpty(left.Id) && left.Id == right.Id);
        }
    }
}
