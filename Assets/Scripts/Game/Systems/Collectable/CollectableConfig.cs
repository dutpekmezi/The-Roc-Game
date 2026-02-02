using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems
{
    public class CollectableConfig : ScriptableObject
    {
        public Sprite Icon;
        [Dropdown("GetCollectableIds")]
        public string Id;
        public string Name;
        public Color Color;

        private List<string> GetCollectableIds()
        {
            return CollectableIds.GetCollectableIds();
        }
    }
}
