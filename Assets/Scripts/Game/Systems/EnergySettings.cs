using UnityEngine;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "EnergySettings", menuName = "Game/Energy/Energy Settings")]
    public class EnergySettings : ScriptableObject
    {
        [Header("Daily Energy")]
        [Min(1)] public int maxEnergy = 15;
        [Min(1)] public int dailyEnergy = 15;
        [Min(1f)] public float dailyRefillHours = 3f;

        [Header("Energy Costs")]
        [Min(0)] public int spinEnergy = 1;
        [Min(0)] public int playEnergy = 1;

        [Header("Daily Spin")]
        [Min(1f)] public float freeSpinCooldownHours = 24f;

        private void OnValidate()
        {
            maxEnergy = Mathf.Max(1, maxEnergy);
            dailyEnergy = Mathf.Clamp(dailyEnergy, 1, maxEnergy);
            dailyRefillHours = Mathf.Max(1f, dailyRefillHours);
            spinEnergy = Mathf.Max(0, spinEnergy);
            playEnergy = Mathf.Max(0, playEnergy);
            freeSpinCooldownHours = Mathf.Max(1f, freeSpinCooldownHours);
        }
    }
}
