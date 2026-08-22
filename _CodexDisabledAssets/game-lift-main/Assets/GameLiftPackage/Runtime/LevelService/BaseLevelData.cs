using UnityEngine;

namespace GameLift.Levels
{
    public abstract class BaseLevelData : ScriptableObject
    {
        [field: SerializeField] public int Index { get; private set; }
    }
}
