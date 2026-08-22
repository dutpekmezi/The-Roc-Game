using System;
using UnityEngine;

namespace Game.Systems
{
    [Serializable]
    public class RewardData
    {
        [SerializeField] private CollectableConfig collectableData;
        [SerializeField, Min(1)] private int amount = 1;

        public CollectableConfig CollectableData => collectableData;
        public int Amount => Mathf.Max(1, amount);
        public string Name => collectableData != null ? collectableData.Name : string.Empty;
    }
}
