using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.Save;
using Utils.Signal;

namespace Utils.Currency
{
    public class CurrenciesEntity : ISaveable
    {
        public Dictionary<string, int> currencies;
        public CurrenciesEntity()
        {
            currencies = new();
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public T Deserialize<T>(string data) where T : ISaveable, new()
        {
            if (String.IsNullOrEmpty(data))
            {
                return JsonConvert.DeserializeObject<T>(null);
            }

            return JsonConvert.DeserializeObject<T>(data);
        }
    }

    public enum Operation
    {
        Add, Substact
    }

    public class CurrencyService : ICurrencyService
    {
        private CurrencyServiceSettings settings;
        private Dictionary<string, float> FakeCurrencyDecrease = new();
        private SaveRepository<CurrenciesEntity> currencyRepo;

        public CurrencyServiceSettings Settings => settings;

        public static CurrencyService Instance { get; private set; }

        public CurrencyService(CurrencyServiceSettings _settings)
        {
            if (Instance != null)
                throw new System.Exception("CurrencyService already has an Instance");

            Instance = this;

            settings = _settings;

            SaveService.Instance.Register<CurrenciesEntity>("currencies");
            currencyRepo = SaveService.Instance.GetRepository<CurrenciesEntity>();
            currencyRepo.Load();
        }

        public bool CanPurchase(string currencyId, int amount)
        {
            CurrenciesEntity ce = currencyRepo.Get();

            if (ce.currencies.ContainsKey(currencyId) && (amount <= ce.currencies[currencyId]))
            {
                return true;
            }

            return false;
        }

        public bool TryPurchase(string currencyId, int amount)
        {
            CurrenciesEntity data = currencyRepo.Get();

            if (data.currencies.ContainsKey(currencyId) && (amount <= data.currencies[currencyId]))
            {
                data.currencies[currencyId] -= amount;
                data.currencies[currencyId] = Mathf.Clamp(data.currencies[currencyId], 0, int.MaxValue);

                currencyRepo.Save(data);

                SignalBus.Get<OnCurrencyChangedSignal>().Invoke(currencyId, GetCurrencyForUI(currencyId));
                SignalBus.Get<OnCurrencyChangedUISignal>().Invoke(currencyId, GetCurrencyForUI(currencyId));

                return true;
            }

            return false;
        }

        public int GetCurrency(string currencyId)
        {
            var data = currencyRepo.Get();

            if (!data.currencies.ContainsKey(currencyId))
            {
                data.currencies.Add(currencyId, 0);

                currencyRepo.Save(data);
            }

            return data.currencies[currencyId];
        }

        public int GetCurrency(CurrencyConfig currencyConfig)
        {
            return GetCurrency(currencyConfig.currencyId);
        }

        public int GetCurrencyForUI(string currencyId)
        {
            var data = currencyRepo.Get();

            if (!data.currencies.ContainsKey(currencyId))
            {
                data.currencies.Add(currencyId, 0);

                currencyRepo.Save(data);
            }

            if (!FakeCurrencyDecrease.ContainsKey(currencyId))
            {
                FakeCurrencyDecrease.Add(currencyId, 0);
            }

            return data.currencies[currencyId] + (int)FakeCurrencyDecrease[currencyId];
        }

        public void AddFakeCurrency(string currencyId, float modify)
        {
            if (FakeCurrencyDecrease.ContainsKey(currencyId))
            {
                FakeCurrencyDecrease[currencyId] += modify;
            }
            else
            {
                FakeCurrencyDecrease.Add(currencyId, modify);
            }

            SignalBus.Get<OnCurrencyChangedUISignal>().Invoke(currencyId, GetCurrencyForUI(currencyId));
        }

        public float GetFakeCurreny(string currencyId)
        {
            if (!FakeCurrencyDecrease.ContainsKey(currencyId))
            {
                FakeCurrencyDecrease.Add(currencyId, 0);
            }

            return FakeCurrencyDecrease[currencyId];
        }

        public void ModifyCurrency(string currencyId, int modify, bool addFakeDecrease = false)
        {
            var data = currencyRepo.Get();

            if (data.currencies.ContainsKey(currencyId))
            {
                data.currencies[currencyId] += modify;
                data.currencies[currencyId] = Mathf.Clamp(data.currencies[currencyId], 0, int.MaxValue);
            }
            else
            {
                if (modify > 0)
                    data.currencies.Add(currencyId, modify);
            }

            if (addFakeDecrease)
            {
                AddFakeCurrency(currencyId, -modify);
            }
            else
            {
                SignalBus.Get<OnCurrencyChangedUISignal>().Invoke(currencyId, GetCurrencyForUI(currencyId));
            }

            currencyRepo.Save(data);

            SignalBus.Get<OnCurrencyChangedSignal>().Invoke(currencyId, data.currencies[currencyId]);
        }

        public void ModifyCurrency(CurrencyConfig currencyConfig, int modify, bool addFakeDecrease = false)
        {
            ModifyCurrency(currencyConfig.currencyId, modify, addFakeDecrease);
        }

        public void ModifyCurrency(Dictionary<string, int> currenciesDict, bool addFakeDecrease = false, Operation operation = Operation.Add)
        {
            var data = currencyRepo.Get();

            foreach (var currency in currenciesDict)
            {
                if (data.currencies.ContainsKey(currency.Key))
                {
                    if (operation == Operation.Add)
                        data.currencies[currency.Key] += currency.Value;
                    else
                        data.currencies[currency.Key] -= currency.Value;

                    data.currencies[currency.Key] = Mathf.Clamp(data.currencies[currency.Key], 0, int.MaxValue);
                }
                else
                {
                    if (operation == Operation.Add)
                    {
                        data.currencies.Add(currency.Key, currency.Value);
                    }
                }

                if (addFakeDecrease)
                {
                    AddFakeCurrency(currency.Key, -data.currencies[currency.Key]);
                }
                else
                {
                    SignalBus.Get<OnCurrencyChangedUISignal>().Invoke(currency.Key, GetCurrencyForUI(currency.Key));

                }

                currencyRepo.Save(data);

                SignalBus.Get<OnCurrencyChangedSignal>().Invoke(currency.Key, data.currencies[currency.Key]);
            }
        }


        public CurrencyConfig GetCurrencyConfig(string currencyId)
        {
            CurrencyConfig cd = settings.currencyConfigs.Find(s => s.currencyId == currencyId);
            return cd;
        }
    }
}