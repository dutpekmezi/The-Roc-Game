using UnityEngine;

namespace Game.Systems
{
    public class StoreSettings
    {
        [SerializeField] private ProductConfigs productConfigs;
        public ProductConfigs ProductConfigs => productConfigs;
    }
}