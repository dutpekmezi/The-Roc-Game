using System.Collections.Generic;
using UnityEngine;
using Utils.Singleton;

namespace Game.Systems
{
    public class SpinRewardSystem : BaseSystem
    {
        [SerializeField] private SpinRewardSettings settings;

        public static SpinRewardSystem Instance { get; private set; }
        public IReadOnlyList<SpinRewardConfig> Rewards => settings != null ? settings.Rewards : null;

        public SpinRewardSystem(SpinRewardSettings spinSettings) 
        {
            if (Instance != null && Instance != this)
            {
                Instance.Dispose();
            }

            Instance = this;

            this.settings = spinSettings;
        }

        public override void Dispose()
        {
            
        }

        public override void Tick()
        {
            
        }

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
