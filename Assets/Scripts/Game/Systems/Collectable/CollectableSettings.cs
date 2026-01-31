using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "CollectableSettings", menuName = "Game/Collectable/Collectable Settings")]
    public class CollectableSettings : ScriptableObject
    {
        public List<Collectable> collectablePrefabs;
    }
}