using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace GameLift.Purchasing
{
    [Serializable]
    public class ProductConfig
    {
        [Header("Product Info")]
        public string ProductId;
        public ProductType Type;
        
        [Header("Shop Item UI")]
        public ShopItemType ShopItemType;
        public Sprite Icon;
        public string DisplayName;
        public BadgeConfig Badge;
    }

    [Serializable]
    public class BadgeConfig
    {
        public string Text;
        public Color BackgroundColor = Color.red;
        public Color TextColor = Color.white;
    }

    public enum ShopItemType
    {
        Normal,
        Premium
    }
}
