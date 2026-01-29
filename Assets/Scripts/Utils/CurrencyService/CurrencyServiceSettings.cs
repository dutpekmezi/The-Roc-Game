using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils.Currency
{
    [CreateAssetMenu(fileName = "CurrencySettings", menuName = "Systems/Currencies/Currency Settings", order = 0)]
    public class CurrencyServiceSettings : ScriptableObject
    {
        public CurrencyConfig hardCurrency;

        public CurrencyConfig softCurrency;

        public CurrencyConfig energyCurrency;

        public List<CurrencyConfig> currencyConfigs;

        public CurrencyConfig GetCurrencyConfig(string currencyId)
        {
            return currencyConfigs.FirstOrDefault(s => s.currencyId == currencyId);
        }
    }
}
