using System.Collections.Generic;

namespace Utils.Currency
{
    public interface ICurrencyService
    {
        public CurrencyServiceSettings Settings { get; }

        public bool CanPurchase(string currencyId, int amount);
        public int GetCurrency(string currencyId);
        public int GetCurrency(CurrencyConfig currencyConfig);
        public int GetCurrencyForUI(string currencyId);

        public void AddFakeCurrency(string currencyId, float modify);
        public float GetFakeCurreny(string currencyId);


        public void ModifyCurrency(string currencyId, int modify, bool addFakeDecrease = false);
        public void ModifyCurrency(CurrencyConfig currencyConfig, int modify, bool addFakeDecrease = false);
        public void ModifyCurrency(Dictionary<string, int> currenciesDict, bool addFakeDecrease = false, Operation operation = Operation.Add);

        public CurrencyConfig GetCurrencyConfig(string currencyId);
    }
}
