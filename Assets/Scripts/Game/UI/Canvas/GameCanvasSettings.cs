using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "GameCanvasSettings", menuName = "Game/UI/Game Canvas Settings")]
    public class GameCanvasSettings : ScriptableObject
    {
        public List<CollectableBar> collectableBars;

        public RectTransform collectableBarsParent;
    }
}
