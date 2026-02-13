using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using Utils.Currency;

namespace Game.Systems
{
    [CreateAssetMenu(fileName = "ProductConfig", menuName = "Game/Product/Product Config")]
    public class ProductConfig : ScriptableObject
    {
        public Sprite Sprite;

        public string Name;
        public string Description;

        [Dropdown("GetCurrencyIds")]
        public string priceCurrency;
        public int priceAmount;

        [Dropdown("GetCurrencyIds")]
        public string specialPriceCurrency;
        public int specialPriceAmount;

        private List<string> GetCurrencyIds()
        {
            return CurrencyIds.GetCurrencyIds();
        }
    }
}