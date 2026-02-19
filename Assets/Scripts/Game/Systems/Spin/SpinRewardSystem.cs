using System.Collections.Generic;
using UnityEngine;
using Utils.Singleton;

namespace Game.Systems
{
    public class SpinRewardSystem : Singleton<SpinRewardSystem>
    {
        [SerializeField] private SpinRewardSettings settings;

        public IReadOnlyList<SpinRewardConfig> Rewards => settings != null ? settings.Rewards : null;

        public bool TryGetReward(int index, out SpinRewardConfig reward)
        {
            reward = null;
            var rewards = Rewards;

            if (rewards == null || index < 0 || index >= rewards.Count)
            {
                return false;
            }

            reward = rewards[index];
            return reward != null;
        }
    }
}
