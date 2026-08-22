using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    public enum SlotSpinCurrencyType
    {
        Energy = 0,
        Coin = 1,
        Coffee = 2,
        Matcha = 3,
        Cookie = 4
    }

    [CreateAssetMenu(fileName = "SlotConfig", menuName = "Game/Slot/Slot Config")]
    public class SlotConfig : ScriptableObject
    {
        [SerializeField] private SlotItem slotItemPrefab;
        [SerializeField] private Vector2 slotItemSize;
        [SerializeField] private SlotSpinCurrencyType spinCurrencyType = SlotSpinCurrencyType.Energy;
        [SerializeField, Min(0)] private int spinCostAmount = 1;
        [SerializeField, Range(0f, 1f)] private float secondSlotsSimilarityRate;
        [SerializeField, Range(0f, 1f)] private float lastSlotSimilarityRate;
        [SerializeField, Range(0f, 1f)] private float firstAndLastSlotsSimilarityRate;
        [SerializeField] private List<SlotReelSpinSettings> reelSettings = new()
        {
            new SlotReelSpinSettings { fullLoopCount = 3, fullLoopDuration = 0.3f, settleDuration = 0.8f, startDelay = 0f },
            new SlotReelSpinSettings { fullLoopCount = 4, fullLoopDuration = 0.3f, settleDuration = 0.9f, startDelay = 0.1f },
            new SlotReelSpinSettings { fullLoopCount = 5, fullLoopDuration = 0.3f, settleDuration = 1f, startDelay = 0.2f }
        };

        public SlotItem SlotItemPrefab => slotItemPrefab;
        public Vector2 SlotItemSize => slotItemSize;
        public bool HasCustomSlotItemWidth => slotItemSize.x > 0f;
        public bool HasCustomSlotItemHeight => slotItemSize.y > 0f;
        public SlotSpinCurrencyType SpinCurrencyType => spinCurrencyType;
        public int SpinCostAmount => Mathf.Max(0, spinCostAmount);
        public float SecondSlotsSimilarityRate => Mathf.Clamp01(secondSlotsSimilarityRate);
        public float LastSlotSimilarityRate => Mathf.Clamp01(lastSlotSimilarityRate);
        public float FirstAndLastSlotsSimilarityRate => Mathf.Clamp01(firstAndLastSlotsSimilarityRate);

        public SlotReelSpinSettings GetReelSettings(int reelIndex)
        {
            if (reelSettings == null || reelSettings.Count == 0)
            {
                return SlotReelSpinSettings.Default;
            }

            int clampedIndex = Mathf.Clamp(reelIndex, 0, reelSettings.Count - 1);
            return reelSettings[clampedIndex] ?? SlotReelSpinSettings.Default;
        }

        private void OnValidate()
        {
            slotItemSize.x = Mathf.Max(0f, slotItemSize.x);
            slotItemSize.y = Mathf.Max(0f, slotItemSize.y);
            spinCostAmount = Mathf.Max(0, spinCostAmount);
            secondSlotsSimilarityRate = Mathf.Clamp01(secondSlotsSimilarityRate);
            lastSlotSimilarityRate = Mathf.Clamp01(lastSlotSimilarityRate);
            firstAndLastSlotsSimilarityRate = Mathf.Clamp01(firstAndLastSlotsSimilarityRate);
        }
    }

    [Serializable]
    public class SlotReelSpinSettings
    {
        [Min(0)] public int fullLoopCount = 3;
        [Min(0.01f)] public float fullLoopDuration = 0.3f;
        [Min(0.01f)] public float settleDuration = 0.85f;
        [Min(0f)] public float startDelay = 0f;

        public static SlotReelSpinSettings Default => new()
        {
            fullLoopCount = 3,
            fullLoopDuration = 0.3f,
            settleDuration = 0.85f,
            startDelay = 0f
        };
    }
}
