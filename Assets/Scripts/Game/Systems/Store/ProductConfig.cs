using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using Utils.Currency;

namespace Game.Systems
{
    public enum ProductSection
    {
        Matcha = 0,
        Coffee = 1,
        Dessert = 2
    }

    [CreateAssetMenu(fileName = "ProductConfig", menuName = "Game/Product/Product Config")]
    public class ProductConfig : ScriptableObject
    {
        [System.Serializable]
        public class ProductPrice
        {
            [Dropdown("GetCurrencyIds")]
            public string currency;
            public int amount;

            private List<string> GetCurrencyIds()
            {
                return CurrencyIds.GetCurrencyIds();
            }
        }

        public Sprite Sprite;

        public string Id;
        public string Name;
        public string Description;

        public List<ProductPrice> prices = new List<ProductPrice>();

        public ProductSection section;

        public IReadOnlyList<ProductPrice> Prices => prices;

        private List<string> GetCurrencyIds()
        {
            return CurrencyIds.GetCurrencyIds();
        }
    }
}
