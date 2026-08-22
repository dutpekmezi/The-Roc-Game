using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "SpinRewardSettings", menuName = "Game/Spin/Spin Reward Settings")]
    public class SpinRewardSettings : ScriptableObject
    {
        public List<RewardData> Rewards = new();
    }
}
