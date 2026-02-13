using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using Utils.Currency;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "ProductConfigs", menuName = "Game/Product/Product Configs")]
    public class ProductConfigs : ScriptableObject
    {
        public List<ProductConfig> configs;
    }
}