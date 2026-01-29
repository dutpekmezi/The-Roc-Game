using UnityEngine;

namespace Utils.Level
{
    [CreateAssetMenu(fileName = "LevelSettings", menuName = "Systems/Levels/Settings")]
    public class LevelSettings : ScriptableObject
    {
        [field: SerializeField] public int LoopMinLevel;
        [field: SerializeField] public int LoopMaxLevel;
    }
}