using UnityEngine;

namespace Utils.Math
{
    [System.Serializable]
    public class LinearProgression
    {
        [field: SerializeField] public float BaseValue = 10f;
        [field: SerializeField] public float GrowthPerLevel = 5f;

        public LinearProgression(float basevalue, float growth)
        {
            this.BaseValue = basevalue;
            this.GrowthPerLevel = growth;
        }

        public float CalculateValueOfLevel(int level)
        {
            float value = BaseValue;
            for (int i = 1; i <= level; i++)
            {
                value += GrowthPerLevel;
            }
            return value;
        }

        public int CalculateLevelOfValue(float value)
        {
            float currentValue = 0;

            for (int i = 1; i <= 1000; i++)
            {
                currentValue += GrowthPerLevel;
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
                currentValue += GrowthPerLevel;

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