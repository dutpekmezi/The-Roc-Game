using Game.UI;
using UnityEngine;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "StoreSettings", menuName = "Game/Store/Store Settings")]
    public class StoreSettings : ScriptableObject
    {
        [SerializeField] private ProductConfigs productConfigs;
        public ProductConfigs ProductConfigs => productConfigs;



        [SerializeField] private ProductCard productCardPrefab;
        public ProductCard ProductCardPrefab => productCardPrefab;
    }
}