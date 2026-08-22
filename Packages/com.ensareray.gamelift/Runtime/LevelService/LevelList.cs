using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GameLift.Levels
{
    [CreateAssetMenu(fileName = "LevelList", menuName = "Game Lift/Level Service/LevelList")]
    public class LevelList : ScriptableObject
    {
        [field: SerializeField] public int LoadImmediateUntil { get; private set; }
        [field: SerializeField] public AssetReferenceT<BaseLevelData> TestLevel { get; private set; }
        [field: SerializeField] public string LevelsFolderPath { get; private set; }
        [field: SerializeField] public List<LevelDataContainer> Levels { get; private set; }
    }

    [System.Serializable]
    public class LevelDataContainer
    {
        [field: SerializeField] public AssetReferenceT<BaseLevelData> Level { get; private set; }
        [field: SerializeField] public bool IsLoopable { get; private set; }
        [field: SerializeField] public LevelDifficulty Difficulty { get; private set; }
    }

    [System.Serializable]
    public enum LevelDifficulty
    {
        Easy,
        Medium,
        Hard
    }
}
