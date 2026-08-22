using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Systems
{
    public class EnergyService
    {
        private const string EnergyAmountPrefsKey = "EnergyService.Amount";
        private const string EnergyCurrencyId = "energy";
        private const int FirebaseReadyPollDelayMs = 100;
        private const int CloudSyncRetryAttempts = 3;
        private const int CloudSyncRetryDelayMs = 1000;

        private readonly EnergySettings settings;
        private readonly FirestoreGameSecurityService firestoreService;
        private readonly object cloudSyncLock = new object();

        private Task initialCloudLoadTask;
        private bool localEnergyInitialized;
        private bool cloudEnergySyncInProgress;
        private int pendingCloudEnergy = -1;

        public static EnergyService Instance { get; private set; }
        public int CurrentEnergy { get; private set; }
        public event Action<int> EnergyChanged;

        public EnergyService(
            EnergySettings settings,
            FirestoreGameSecurityService firestoreService)
        {
            Instance = this;
            this.settings = settings;
            this.firestoreService = firestoreService;
        }

        public Task InitializeFromFirebaseAsync(bool forceRefresh = false)
        {
            InitializeLocalEnergy();

            if (!forceRefresh && initialCloudLoadTask != null)
            {
                return initialCloudLoadTask;
            }

            initialCloudLoadTask = LoadInitialEnergyFromFirebaseAsync();
            _ = ObserveInitialCloudLoadAsync(initialCloudLoadTask);
            return initialCloudLoadTask;
        }

        public async Task<bool> TrySpendForPlayAsync()
        {
            await WaitForInitialCloudLoadAsync();

            FirestoreGameSecurityService firebase = await GetReadyFirebaseServiceAsync();
            if (firebase == null)
            {
                Debug.LogWarning("[EnergyService] Firebase hazir degil; run baslatilamadi.");
#if UNITY_EDITOR
                return TrySpendLocalEnergy(settings.playEnergy, "oyuna baslamak", syncCloud: false);
#else
                return false;
#endif
            }

            RunStartResult result = await firebase.TryStartRunAsync(settings.playEnergy);
            if (result.IsSuccess)
            {
                SetEnergy(result.EnergyBalance);
                GameState.Instance?.SetActiveRunId(result.RunId);
                Debug.Log("[EnergyService] Server run baslatildi. runId=" + result.RunId + ", energy=" + result.EnergyBalance);
                return true;
            }

            if (result.IsInsufficient)
            {
                SetEnergy(result.EnergyBalance);
                Debug.LogWarning("[EnergyService] Server enerji yetersiz dedi. balance=" + result.EnergyBalance);
                return false;
            }

            Debug.LogWarning("[EnergyService] Server run baslatma reddedildi: " + result.Error);
#if UNITY_EDITOR
            Debug.LogWarning("[EnergyService] Editor fallback: Cloud Function hazir olmadigi icin local-only enerjiyle baslatiliyor. Firebase coin/energy yazimi icin functions deploy gerekli.");
            return TrySpendLocalEnergy(settings.playEnergy, "oyuna baslamak", syncCloud: false);
#else
            return false;
#endif
        }

        public async Task<bool> TrySpendForRunStartAsync()
        {
            try
            {
                return await TrySpendForPlayAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[EnergyService] Enerji harcama kontrolu tamamlanamadi: " + e.Message);
                return false;
            }
        }

        public Task<bool> TryConsumeSpinAsync()
        {
            return TryConsumeSpinAsync(syncCloud: true);
        }

        public async Task<bool> TryConsumeSpinAsync(bool syncCloud)
        {
            return await TryConsumeSpinAsync(settings.spinEnergy, syncCloud);
        }

        public Task<bool> TryConsumeSpinAsync(int amount)
        {
            return TryConsumeSpinAsync(amount, syncCloud: true);
        }

        public async Task<bool> TryConsumeSpinAsync(int amount, bool syncCloud)
        {
            await WaitForInitialCloudLoadAsync();

            if (amount <= 0)
            {
                return true;
            }

            if (syncCloud)
            {
                FirestoreGameSecurityService firebase = await GetReadyFirebaseServiceAsync();
                if (firebase != null)
                {
                    try
                    {
                        EnergyTransactionResult result = await firebase.TrySpendEnergyAsync(amount);
                        if (result.IsSuccess)
                        {
                            SetEnergy(result.Balance);
                            return true;
                        }

                        if (result.IsInsufficient)
                        {
                            SetEnergy(result.Balance);
                            Debug.LogWarning("[EnergyService] Server spin enerjisi yetersiz dedi. balance=" + result.Balance);
                            return false;
                        }

                        Debug.LogWarning("[EnergyService] Server spin enerji harcama reddedildi: " + result.Error);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[EnergyService] Server spin enerji harcama hata: " + e.Message);
                    }
                }

#if !UNITY_EDITOR
                return false;
#endif
            }

            return TrySpendLocalEnergy(amount, "spin cevirmek", syncCloud);
        }

        public void AddEnergy(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            SetEnergy(CurrentEnergy + amount);
            QueueEnergyCloudSync(CurrentEnergy);
        }

        public void SetClientEnergy(int amount)
        {
            SetEnergy(amount);
            QueueEnergyCloudSync(CurrentEnergy);
        }

        public void ApplyServerEnergy(int amount)
        {
            SetEnergy(amount);
        }

        private async Task ObserveInitialCloudLoadAsync(Task syncTask)
        {
            try
            {
                await syncTask;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[EnergyService] Initial Firebase energy sync skipped: " + e.Message);
            }
        }

        private void BeginInitialCloudLoad()
        {
            _ = InitializeFromFirebaseAsync();
        }

        private async Task WaitForInitialCloudLoadAsync()
        {
            BeginInitialCloudLoad();

            Task syncTask = initialCloudLoadTask;
            if (syncTask == null)
            {
                return;
            }

            try
            {
                await syncTask;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[EnergyService] Initial Firebase energy sync wait failed: " + e.Message);
            }
        }

        private async Task LoadInitialEnergyFromFirebaseAsync()
        {
            FirestoreGameSecurityService firebase = await GetReadyFirebaseServiceAsync();
            if (firebase == null)
            {
                Debug.LogWarning("[EnergyService] Firebase not ready on startup; client energy remains at 0.");
                return;
            }

            try
            {
                EnergyTransactionResult energyResult = await firebase.GetEnergyAmountAsync(settings.maxEnergy);
                if (energyResult.IsSuccess)
                {
                    ApplyInitialCloudEnergy(energyResult.Balance, "Startup energy loaded from Firebase energy document");
                    return;
                }

                Debug.LogWarning("[EnergyService] Startup direct Firebase energy read failed: " + energyResult.Error);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[EnergyService] Startup direct Firebase energy read threw: " + e.Message);
            }

            Dictionary<string, int> currencies = await firebase.GetCurrencyAmountsAsync();
            if (currencies != null && currencies.TryGetValue(EnergyCurrencyId, out int energyAmount))
            {
                ApplyInitialCloudEnergy(energyAmount, "Startup energy loaded from Firebase snapshot");
                return;
            }

            if (currencies != null)
            {
                ApplyInitialCloudEnergy(0, "Startup energy missing in Firebase, defaulted to");
                return;
            }

            Debug.LogWarning("[EnergyService] Startup Firebase currencies could not be read; client energy remains at 0.");
        }

        private void ApplyInitialCloudEnergy(int energyAmount, string logPrefix)
        {
            int previousEnergy = CurrentEnergy;
            SetEnergy(energyAmount);
            Debug.Log("[EnergyService] " + logPrefix + ": " + CurrentEnergy + " (previous local=" + previousEnergy + ")");
        }

        private bool TrySpendLocalEnergy(int amount, string reason, bool syncCloud = true)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (CurrentEnergy < amount)
            {
                Debug.LogWarning($"[EnergyService] {reason} icin yeterli enerji yok. Gerekli: {amount}, mevcut: {CurrentEnergy}");
                return false;
            }

            SetEnergy(CurrentEnergy - amount);
            if (syncCloud)
            {
                QueueEnergyCloudSync(CurrentEnergy);
            }

            return true;
        }

        private void QueueEnergyCloudSync(int energyAmount)
        {
            int clampedEnergy = Mathf.Clamp(energyAmount, 0, settings.maxEnergy);

            lock (cloudSyncLock)
            {
                pendingCloudEnergy = clampedEnergy;
                if (cloudEnergySyncInProgress)
                {
                    return;
                }

                cloudEnergySyncInProgress = true;
            }

            _ = DrainEnergyCloudSyncQueueAsync();
        }

        private async Task DrainEnergyCloudSyncQueueAsync()
        {
            while (true)
            {
                int targetEnergy;
                lock (cloudSyncLock)
                {
                    if (pendingCloudEnergy < 0)
                    {
                        cloudEnergySyncInProgress = false;
                        return;
                    }

                    targetEnergy = pendingCloudEnergy;
                    pendingCloudEnergy = -1;
                }

                await SyncEnergyAmountToFirebaseAsync(targetEnergy);
            }
        }

        private async Task SyncEnergyAmountToFirebaseAsync(int targetEnergy)
        {
            for (int attempt = 0; attempt < CloudSyncRetryAttempts; attempt++)
            {
                try
                {
                    FirestoreGameSecurityService firebase = await GetReadyFirebaseServiceAsync();
                    if (firebase == null)
                    {
                        Debug.LogWarning("[EnergyService] Firebase not ready; energy cloud sync skipped. energy=" + targetEnergy);
                        return;
                    }

                    await firebase.SyncCurrencyAmountAsync(EnergyCurrencyId, targetEnergy);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[EnergyService] Energy cloud sync failed: " + e.Message);
                }

                if (attempt < CloudSyncRetryAttempts - 1)
                {
                    await UniTask.Delay(CloudSyncRetryDelayMs);
                }
            }
        }

        private async Task<FirestoreGameSecurityService> GetReadyFirebaseServiceAsync()
        {
            for (;;)
            {
                FirestoreGameSecurityService service = GetFirebaseServiceReference();
                if (service != null)
                {
                    bool isReady = service.IsReady || await service.InitializeServiceAsync();
                    return isReady ? service : null;
                }

                await UniTask.Delay(FirebaseReadyPollDelayMs);
            }
        }

        private FirestoreGameSecurityService GetFirebaseServiceReference()
        {
            return firestoreService != null ? firestoreService : FirestoreGameSecurityService.Instance;
        }

        private void InitializeLocalEnergy()
        {
            if (localEnergyInitialized)
            {
                return;
            }

            localEnergyInitialized = true;
            SetEnergy(0);
        }

        private void SetEnergy(int amount)
        {
            int nextEnergy = Mathf.Clamp(amount, 0, settings.maxEnergy);
            bool changed = CurrentEnergy != nextEnergy;
            CurrentEnergy = nextEnergy;

            PlayerPrefs.SetInt(GetScopedPrefsKey(EnergyAmountPrefsKey), CurrentEnergy);
            PlayerPrefs.Save();

            if (changed)
            {
                EnergyChanged?.Invoke(CurrentEnergy);
            }
        }

        private string GetScopedPrefsKey(string baseKey)
        {
            string userId = firestoreService != null
                ? firestoreService.GetUserId()
                : FirestoreGameSecurityService.Instance?.GetUserId();

            return string.IsNullOrEmpty(userId) ? baseKey : baseKey + "." + userId;
        }
    }
}
