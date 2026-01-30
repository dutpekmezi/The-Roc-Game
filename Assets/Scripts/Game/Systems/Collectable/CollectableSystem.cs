using Game.Systems;
using UnityEngine;

namespace Game.Systems
{
    public class CollectableSystem : BaseSystem
    {
        public CollectableSettings CollectableSettings { get; private set; }

        public CollectableSystem(CollectableSettings collectableSettings)
        {
            CollectableSettings = collectableSettings;
        }

        public override void Tick()
        {
            
        }
    }
}