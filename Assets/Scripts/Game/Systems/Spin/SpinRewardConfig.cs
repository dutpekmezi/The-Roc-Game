using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using Utils.Currency;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "SpinRewardConfig", menuName = "Game/Spin/Spin Reward Config")]
    public class SpinRewardConfig : ScriptableObject
    {
        [Dropdown("GetCurrencyIds")]
        public string Id;
        public string RewardName;
        public int Amount;
        public Color Color = Color.white;
        public Sprite Icon;
        public Sprite Background;

        private List<string> GetCurrencyIds()
        {
            return CurrencyIds.GetCurrencyIds();
        }
    }
}
