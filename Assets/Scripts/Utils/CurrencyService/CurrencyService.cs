using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils.Save;
using Utils.Signal;
using VContainer;

namespace Utils.Currency
{
    public class CurrenciesEntity : ISaveable
    {
        public Dictionary<string, int> currencies;

        public CurrenciesEntity()
        {
            currencies = new Dictionary<string, int>();
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public T Deserialize<T>(string data) where T : ISaveable, new()
        {
            if (string.IsNullOrEmpty(data))
            {
                return new T();
            }

            return JsonConvert.DeserializeObject<T>(data);
        }
    }

    public enum Operation
    {
        Add,
        Substact
    }

    public class CurrencyService : ICurrencyService
    {
        private const string LocalCurrencyUserIdPrefsKey = "CurrencyService.LastUserId";
        private const string EnergyCurrencyId = "energy";
        private const int FirebaseReadyPollDelayMs = 100;
        private const int CloudDeltaRetryAttempts = 3;
        private const int CloudDeltaRetryDelayMs = 1000;

        private readonly FirestoreGameSecurityService firestoreService;
        private readonly SaveRepository<CurrenciesEntity> currencyRepo;
        private readonly Dictionary<string, float> fakeCurrencyDecrease = new Dictionary<string, float>();
        private readonly HashSet<string> cloudDeltaSyncsInProgress = new HashSet<string>();

        private CurrencyServiceSettings settings;
        private Task<bool> cloudRefreshTask;

        public CurrencyServiceSettings Settings => settings;
        public Task CloudRefreshTask => cloudRefreshTask ?? Task.CompletedTask;

        public static CurrencyService Instance { get; private set; }

        public CurrencyService(CurrencyServiceSettings settings)
            : this(settings, SaveService.Instance, null)
        {
        }

        public CurrencyService(CurrencyServiceSettings settings, ISaveService saveService)
            : this(settings, saveService, null)
        {
        }

        [Inject]
        public CurrencyService(
            CurrencyServiceSettings settings,
            ISaveService saveService,
            FirestoreGameSecurityService firestoreService)
        {
            if (Instance != null)
            {
                throw new Exception("CurrencyService already has an Instance");
            }

            Instance = this;
            this.settings = settings;
            this.firestoreService = firestoreService;

            saveService.Register<CurrenciesEntity>("currencies");
            currencyRepo = saveService.GetRepository<CurrenciesEntity>();
            currencyRepo.Load();

            EnsureConfiguredCurrenciesExist();
            cloudRefreshTask = Task.FromResult(false);
        }

        public async Task<bool> RefreshFromFirebaseAsync()
        {
            cloudRefreshTask = RefreshFromFirebaseInternalAsync();
            return await cloudRefreshTask;
        }

        private async Task<bool> RefreshFromFirebaseInternalAsync()
        {
            FirestoreGameSecurityService firebaseService =
                await WaitForFirebaseServiceAsync(firestoreService);

            if (firebaseService == null)
            {
                Debug.LogWarning("[CurrencyService] Firebase hazir degil; server currency snapshot'i alinamadi.");
                return false;
            }

            string currentUserId = firebaseService.GetUserId();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                SaveCurrencyUserId(currentUserId);
            }

            Dictionary<string, int> serverCurrencies = await firebaseService.GetCurrencyAmountsAsync();
            if (serverCurrencies == null || serverCurrencies.Count == 0)
            {
                Debug.LogWarning("[CurrencyService] Firebase currency snapshot'i bos veya okunamadi; direct document fallback deneniyor.");
                serverCurrencies = await ReadConfiguredCurrenciesDirectlyAsync(firebaseService);
                if (serverCurrencies == null)
                {
                    Debug.LogWarning("[CurrencyService] Firebase currency verisi okunamadi.");
                    return false;
                }
            }

            ApplyServerCurrencySnapshot(serverCurrencies);
            await EnsureMissingConfiguredCurrenciesExistInFirebaseAsync(firebaseService, serverCurrencies);
            return true;
        }

        public bool CanPurchase(string currencyId, int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            return GetCurrency(currencyId) >= amount;
        }

        public bool TryPurchase(string currencyId, int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            CurrenciesEntity data = GetData();
            EnsureCurrencyKey(data, currencyId);

            if (data.currencies[currencyId] < amount)
            {
                return false;
            }

            data.currencies[currencyId] = Mathf.Clamp(data.currencies[currencyId] - amount, 0, int.MaxValue);
            currencyRepo.Save(data);
            ClearFakeCurrency(currencyId);
            InvokeCurrencyChanged(currencyId, data.currencies[currencyId]);
            QueueCloudCurrencyDelta(currencyId, -amount);
            return true;
        }

        public int GetCurrency(string currencyId)
        {
            CurrenciesEntity data = GetData();
            EnsureCurrencyKey(data, currencyId);
            currencyRepo.Save(data);
            return data.currencies[currencyId];
        }

        public int GetCurrency(CurrencyConfig currencyConfig)
        {
            return currencyConfig == null ? 0 : GetCurrency(currencyConfig.currencyId);
        }

        public int GetCurrencyForUI(string currencyId)
        {
            CurrenciesEntity data = GetData();
            EnsureCurrencyKey(data, currencyId);
            currencyRepo.Save(data);

            if (!fakeCurrencyDecrease.ContainsKey(currencyId))
            {
                fakeCurrencyDecrease[currencyId] = 0;
            }

            return Mathf.Clamp(data.currencies[currencyId] + (int)fakeCurrencyDecrease[currencyId], 0, int.MaxValue);
        }

        public void AddFakeCurrency(string currencyId, float modify)
        {
            if (string.IsNullOrEmpty(currencyId))
            {
                return;
            }

            if (!fakeCurrencyDecrease.ContainsKey(currencyId))
            {
                fakeCurrencyDecrease[currencyId] = 0;
            }

            fakeCurrencyDecrease[currencyId] += modify;
            SignalBus.Get<OnCurrencyChangedUISignal>().Invoke(currencyId, GetCurrencyForUI(currencyId));
        }

        public float GetFakeCurreny(string currencyId)
        {
            if (string.IsNullOrEmpty(currencyId))
            {
                return 0;
            }

            if (!fakeCurrencyDecrease.ContainsKey(currencyId))
            {
                fakeCurrencyDecrease[currencyId] = 0;
            }

            return fakeCurrencyDecrease[currencyId];
        }

        public void ModifyCurrency(string currencyId, int modify, bool addFakeDecrease = false)
        {
            if (string.IsNullOrEmpty(currencyId) || currencyId == EnergyCurrencyId || modify == 0)
            {
                return;
            }

            CurrenciesEntity data = GetData();
            EnsureCurrencyKey(data, currencyId);

            data.currencies[currencyId] = Mathf.Clamp(data.currencies[currencyId] + modify, 0, int.MaxValue);
            currencyRepo.Save(data);

            if (addFakeDecrease)
            {
                AddFakeCurrency(currencyId, -modify);
            }
            else
            {
                SignalBus.Get<OnCurrencyChangedUISignal>().Invoke(currencyId, GetCurrencyForUI(currencyId));
            }

            SignalBus.Get<OnCurrencyChangedSignal>().Invoke(currencyId, data.currencies[currencyId]);
            QueueCloudCurrencyDelta(currencyId, modify);
        }

        public void ModifyCurrency(CurrencyConfig currencyConfig, int modify, bool addFakeDecrease = false)
        {
            if (currencyConfig == null)
            {
                return;
            }

            ModifyCurrency(currencyConfig.currencyId, modify, addFakeDecrease);
        }

        public void ModifyCurrency(
            Dictionary<string, int> currenciesDict,
            bool addFakeDecrease = false,
            Operation operation = Operation.Add)
        {
            if (currenciesDict == null)
            {
                return;
            }

            foreach (var currency in currenciesDict)
            {
                int delta = operation == Operation.Add ? currency.Value : -currency.Value;
                ModifyCurrency(currency.Key, delta, addFakeDecrease);
            }
        }

        public CurrencyConfig GetCurrencyConfig(string currencyId)
        {
            if (settings?.currencyConfigs == null)
            {
                return null;
            }

            return settings.currencyConfigs.Find(config => config != null && config.currencyId == currencyId);
        }

        public void ApplyServerCurrencySnapshot(Dictionary<string, int> serverCurrencies)
        {
            if (serverCurrencies == null)
            {
                return;
            }

            CurrenciesEntity data = GetData();
            data.currencies.Remove(EnergyCurrencyId);

            var changedCurrencyIds = new HashSet<string>();

            foreach (var currency in serverCurrencies)
            {
                if (string.IsNullOrEmpty(currency.Key) || currency.Key == EnergyCurrencyId)
                {
                    continue;
                }

                int amount = Mathf.Clamp(currency.Value, 0, int.MaxValue);
                bool hadFakeCurrency = HasFakeCurrency(currency.Key);
                if (!data.currencies.TryGetValue(currency.Key, out int currentAmount) || currentAmount != amount)
                {
                    data.currencies[currency.Key] = amount;
                    changedCurrencyIds.Add(currency.Key);
                }
                else if (hadFakeCurrency)
                {
                    changedCurrencyIds.Add(currency.Key);
                }

                ClearFakeCurrency(currency.Key);
            }

            if (settings?.currencyConfigs != null)
            {
                for (int i = 0; i < settings.currencyConfigs.Count; i++)
                {
                    CurrencyConfig config = settings.currencyConfigs[i];
                    if (config == null || string.IsNullOrEmpty(config.currencyId))
                    {
                        continue;
                    }

                    if (!serverCurrencies.TryGetValue(config.currencyId, out int serverAmount))
                    {
                        serverAmount = 0;
                    }

                    serverAmount = Mathf.Clamp(serverAmount, 0, int.MaxValue);
                    bool hadFakeCurrency = HasFakeCurrency(config.currencyId);
                    if (!data.currencies.TryGetValue(config.currencyId, out int currentAmount) ||
                        currentAmount != serverAmount)
                    {
                        data.currencies[config.currencyId] = serverAmount;
                        changedCurrencyIds.Add(config.currencyId);
                    }
                    else if (hadFakeCurrency)
                    {
                        changedCurrencyIds.Add(config.currencyId);
                    }

                    ClearFakeCurrency(config.currencyId);
                }
            }

            currencyRepo.Save(data);

            foreach (string currencyId in changedCurrencyIds)
            {
                InvokeCurrencyChanged(currencyId, data.currencies[currencyId]);
            }
        }

        private void EnsureConfiguredCurrenciesExist()
        {
            CurrenciesEntity data = GetData();
            data.currencies.Remove(EnergyCurrencyId);

            if (settings?.currencyConfigs != null)
            {
                for (int i = 0; i < settings.currencyConfigs.Count; i++)
                {
                    CurrencyConfig config = settings.currencyConfigs[i];
                    if (config == null || string.IsNullOrEmpty(config.currencyId))
                    {
                        continue;
                    }

                    EnsureCurrencyKey(data, config.currencyId);
                }
            }

            currencyRepo.Save(data);
        }

        private async Task<Dictionary<string, int>> ReadConfiguredCurrenciesDirectlyAsync(
            FirestoreGameSecurityService firebaseService)
        {
            if (firebaseService == null || settings?.currencyConfigs == null)
            {
                return null;
            }

            var currencies = new Dictionary<string, int>();

            for (int i = 0; i < settings.currencyConfigs.Count; i++)
            {
                CurrencyConfig config = settings.currencyConfigs[i];
                if (config == null ||
                    string.IsNullOrEmpty(config.currencyId) ||
                    config.currencyId == EnergyCurrencyId)
                {
                    continue;
                }

                CurrencyMutationResult result = await firebaseService.GetCurrencyAmountAsync(config.currencyId);
                if (!result.IsSuccess)
                {
                    Debug.LogWarning("[CurrencyService] Direct currency read failed. currency=" + config.currencyId + ", error=" + result.Error);
                    return null;
                }

                currencies[config.currencyId] = Mathf.Clamp(result.Balance, 0, int.MaxValue);
            }

            return currencies;
        }

        private async Task EnsureMissingConfiguredCurrenciesExistInFirebaseAsync(
            FirestoreGameSecurityService firebaseService,
            Dictionary<string, int> serverCurrencies)
        {
            if (firebaseService == null || settings?.currencyConfigs == null)
            {
                return;
            }

            for (int i = 0; i < settings.currencyConfigs.Count; i++)
            {
                CurrencyConfig config = settings.currencyConfigs[i];
                if (config == null ||
                    string.IsNullOrEmpty(config.currencyId) ||
                    config.currencyId == EnergyCurrencyId ||
                    serverCurrencies.ContainsKey(config.currencyId))
                {
                    continue;
                }

                await firebaseService.SyncCurrencyAmountAsync(config.currencyId, 0);
            }
        }

        private void QueueCloudCurrencyDelta(string currencyId, int delta)
        {
            if (string.IsNullOrEmpty(currencyId) || currencyId == EnergyCurrencyId || delta == 0)
            {
                return;
            }

            if (delta > 0)
            {
                Debug.LogWarning("[CurrencyService] Positive currency cloud delta is blocked; use server reward claim. currency=" + currencyId);
                return;
            }

            _ = SyncCurrencyDeltaToCloudAsync(currencyId, delta);
        }

        private async Task SyncCurrencyDeltaToCloudAsync(string currencyId, int delta)
        {
            string syncKey = currencyId + ":" + delta + ":" + Guid.NewGuid().ToString("N");
            if (!cloudDeltaSyncsInProgress.Add(syncKey))
            {
                return;
            }

            try
            {
                for (int attempt = 0; attempt < CloudDeltaRetryAttempts; attempt++)
                {
                    try
                    {
                        FirestoreGameSecurityService firebaseService =
                            await WaitForFirebaseServiceAsync(firestoreService);

                        if (firebaseService == null)
                        {
                            Debug.LogWarning("[CurrencyService] Firebase hazir degil; currency delta cloud'a yazilamadi. currency=" + currencyId);
                            break;
                        }

                        CurrencyMutationResult result =
                            await firebaseService.ModifyCurrencyAmountAsync(currencyId, delta);

                        if (result.IsSuccess || result.IsInsufficient)
                        {
                            SetLocalCurrencyAmount(currencyId, result.Balance);
                            return;
                        }

                        Debug.LogWarning("[CurrencyService] Currency delta cloud'a yazilamadi: " + result.Error);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[CurrencyService] Currency delta sync hata: " + e.Message);
                    }

                    if (attempt < CloudDeltaRetryAttempts - 1)
                    {
                        await UniTask.Delay(CloudDeltaRetryDelayMs);
                    }
                }

                await RefreshFromFirebaseAsync();
            }
            finally
            {
                cloudDeltaSyncsInProgress.Remove(syncKey);
            }
        }

        private void SetLocalCurrencyAmount(string currencyId, int amount)
        {
            if (string.IsNullOrEmpty(currencyId) || currencyId == EnergyCurrencyId)
            {
                return;
            }

            CurrenciesEntity data = GetData();
            data.currencies[currencyId] = Mathf.Clamp(amount, 0, int.MaxValue);
            currencyRepo.Save(data);
            ClearFakeCurrency(currencyId);
            InvokeCurrencyChanged(currencyId, data.currencies[currencyId]);
        }

        private CurrenciesEntity GetData()
        {
            CurrenciesEntity data = currencyRepo.Get();
            if (data == null)
            {
                data = new CurrenciesEntity();
            }

            if (data.currencies == null)
            {
                data.currencies = new Dictionary<string, int>();
            }

            return data;
        }

        private static void EnsureCurrencyKey(CurrenciesEntity data, string currencyId)
        {
            if (data == null || data.currencies == null || string.IsNullOrEmpty(currencyId))
            {
                return;
            }

            if (!data.currencies.ContainsKey(currencyId))
            {
                data.currencies[currencyId] = 0;
            }
        }

        private void InvokeCurrencyChanged(string currencyId, int amount)
        {
            SignalBus.Get<OnCurrencyChangedSignal>().Invoke(currencyId, amount);
            SignalBus.Get<OnCurrencyChangedUISignal>().Invoke(currencyId, GetCurrencyForUI(currencyId));
        }

        private void ClearFakeCurrency(string currencyId)
        {
            if (!string.IsNullOrEmpty(currencyId))
            {
                fakeCurrencyDecrease[currencyId] = 0;
            }
        }

        private bool HasFakeCurrency(string currencyId)
        {
            return !string.IsNullOrEmpty(currencyId) &&
                   fakeCurrencyDecrease.TryGetValue(currencyId, out float fakeAmount) &&
                   System.Math.Abs(fakeAmount) > 0.001f;
        }

        private static void SaveCurrencyUserId(string userId)
        {
            PlayerPrefs.SetString(LocalCurrencyUserIdPrefsKey, userId);
            PlayerPrefs.Save();
        }

        private static async Task<FirestoreGameSecurityService> WaitForFirebaseServiceAsync(
            FirestoreGameSecurityService preferredService = null)
        {
            for (;;)
            {
                FirestoreGameSecurityService service = preferredService != null
                    ? preferredService
                    : FirestoreGameSecurityService.Instance;

                if (service != null)
                {
                    bool isReady = service.IsReady || await service.InitializeServiceAsync();
                    return isReady ? service : null;
                }

                await UniTask.Delay(FirebaseReadyPollDelayMs);
            }
        }
    }
}
