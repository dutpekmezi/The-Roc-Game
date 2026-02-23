using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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

        [FormerlySerializedAs("priceCurrency")]
        [SerializeField] private string legacyPriceCurrency;
        [FormerlySerializedAs("priceAmount")]
        [SerializeField] private int legacyPriceAmount;

        public List<ProductPrice> prices = new List<ProductPrice>();

        [Dropdown("GetCurrencyIds")]
        public string specialPriceCurrency;
        public int specialPriceAmount;

        public ProductSection section;

        public IReadOnlyList<ProductPrice> Prices
        {
            get
            {
                if (prices != null && prices.Count > 0)
                {
                    return prices;
                }

                return fallbackPrices;
            }
        }

        public string PriceCurrency => Prices.Count > 0 ? Prices[0].currency : string.Empty;
        public int PriceAmount => Prices.Count > 0 ? Prices[0].amount : 0;

        private readonly List<ProductPrice> fallbackPrices = new List<ProductPrice>();

        private List<string> GetCurrencyIds()
        {
            return CurrencyIds.GetCurrencyIds();
        }

        private void OnEnable()
        {
            SyncLegacyPrice();
        }

        private void OnValidate()
        {
            SyncLegacyPrice();
        }

        private void SyncLegacyPrice()
        {
            fallbackPrices.Clear();

            if (!string.IsNullOrEmpty(legacyPriceCurrency))
            {
                fallbackPrices.Add(new ProductPrice
                {
                    currency = legacyPriceCurrency,
                    amount = legacyPriceAmount
                });

                if (prices == null || prices.Count == 0)
                {
                    prices = new List<ProductPrice>(fallbackPrices);
                }
            }
        }
    }
}
