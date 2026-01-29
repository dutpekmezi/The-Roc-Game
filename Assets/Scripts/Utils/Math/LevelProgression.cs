using UnityEngine;

namespace Utils.Math
{
    [System.Serializable]
    public class LevelProgression
    {
        [field: SerializeField] public float BaseValue = 10;
        [field: SerializeField] public float GrowthFactor = 1.1f;

        public LevelProgression(float baseVal, float growth)
        {
            BaseValue = baseVal;
            GrowthFactor = growth;
        }

        public float CalculateValueOfLevel(int level)
        {
            float value = 0;
            for (int i = 1; i <= level; i++)
            {
                value += CalculateRequiredValueForNextLevel(i);
            }

            return value;
        }

        public float CalculateRequiredValueForNextLevel(int currentLevel)
        {
            return (BaseValue * (Mathf.Pow(currentLevel, GrowthFactor)));
        }

        public int CalculateLevelOfValue(float value)
        {
            float currentValue = 0;

            for (int i = 1; i <= 1000; i++)
            {
                currentValue += CalculateRequiredValueForNextLevel(i);

                if (currentValue > value)
                    return i;
            }

            return 1000;
        }

        public float CalculateLevelProgress(float value)
        {
            float currentValue = 0;
            float lastLevelValue = 0;

            for (int i = 1; i <= 1000; i++)
            {
                currentValue += CalculateRequiredValueForNextLevel(i);

                if (currentValue > value)
                {
                    return (value - lastLevelValue) / (currentValue - lastLevelValue);
                }
                lastLevelValue = currentValue;
            }

            return 0;
        }
    }
}