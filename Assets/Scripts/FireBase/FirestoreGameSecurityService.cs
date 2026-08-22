using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
#if !UNITY_WEBGL || UNITY_EDITOR
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif
using Game.Systems;
using GameLift.Audio;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
public class FirestoreGameSecurityService
{
    private const string LocalUserIdPrefsKey = "FirestoreGameSecurityService.WebGLUserId";
    private const int PollDelayMs = 100;
    private const int FirebaseInitProgressLogIntervalAttempts = 300;

    public static FirestoreGameSecurityService Instance { get; private set; }

    public bool IsReady { get; private set; }

    private string activeUserId;
    private readonly IAudioService audioService;
    private readonly SoundData purchaseSound;
    private Task<bool> initializationTask;

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void RocFirebase_BeginInitialize();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_GetInitState();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern string RocFirebase_GetUserId();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern string RocFirebase_GetLastError();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_EnsureUserDocument();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_ClearCurrentUserData();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_TryPurchaseProduct(string productJson);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_ClaimSlotProductReward(string productJson);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_StartRun(int playEnergyCost);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_ClaimRunRewards(string payloadJson);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_ClaimSpinReward(int segmentCount);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_GetShopConfig();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_SyncCurrencyAmount(string currencyId, int amount);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_ModifyCurrencyAmount(string currencyId, int delta);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_GetCurrencyAmounts();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_GetCurrencyAmount(string currencyId);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_GetEnergyAmount(int maxEnergy);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_ClaimDailyEnergy(int maxEnergy, int dailyEnergy, float cooldownHours);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_TrySpendEnergy(int amount);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_TryUseFreeSpin(float cooldownHours);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_GetActivePurchasedProducts();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern int RocFirebase_GetOperationState(int operationId);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern string RocFirebase_GetOperationResultJson(int operationId);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern string RocFirebase_GetOperationError(int operationId);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void RocFirebase_ReleaseOperation(int operationId);

    public FirestoreGameSecurityService()
    {
        Instance = this;
        activeUserId = PlayerPrefs.GetString(LocalUserIdPrefsKey, string.Empty);
    }

    public FirestoreGameSecurityService(IAudioService audioService, SoundData purchaseSound)
        : this()
    {
        this.audioService = audioService;
        this.purchaseSound = purchaseSound;
    }

    public Task<bool> InitializeServiceAsync()
    {
        if (IsReady)
        {
            return Task.FromResult(true);
        }

        if (initializationTask == null || initializationTask.IsCompleted)
        {
            initializationTask = InitializeAsync();
        }

        return initializationTask;
    }

    public string GetUserId()
    {
        return activeUserId;
    }

    public async Task ClearCurrentUserDataAsync()
    {
        if (!await EnsureInitializedAsync())
        {
            return;
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_ClearCurrentUserData(),
            "Clear current WebGL user data");

        WebBridgeUserResult result = FromJson<WebBridgeUserResult>(json);
        if (result != null && !string.IsNullOrEmpty(result.userId))
        {
            activeUserId = result.userId;
            SaveLocalUserId(activeUserId);
        }
    }

    public async Task EnsureUserDocumentAsync(string userId)
    {
        if (!await EnsureInitializedAsync())
        {
            return;
        }

        await WaitForOperationJsonAsync(
            RocFirebase_EnsureUserDocument(),
            "Ensure WebGL user document");
    }

    public async Task<PurchaseResult> TryPurchaseProductAsync(ProductConfig productConfig)
    {
        if (!await EnsureInitializedAsync())
        {
            return PurchaseResult.Failed("Firebase hazir degil");
        }

        if (productConfig == null || string.IsNullOrEmpty(productConfig.Id))
        {
            return PurchaseResult.Failed("Gecersiz urun");
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_TryPurchaseProduct(BuildProductPayloadJson(productConfig)),
            "WebGL purchase transaction");

        WebBridgePurchaseResult result = FromJson<WebBridgePurchaseResult>(json);
        if (result == null)
        {
            return PurchaseResult.Failed("Transaction hata");
        }

        if (!result.isSuccess)
        {
            return PurchaseResult.Failed(result.error);
        }

        if (purchaseSound != null)
        {
            audioService?.Play(purchaseSound);
        }

        return PurchaseResult.Success(result.qrPayload);
    }

    public async Task<PurchaseResult> ClaimSlotProductRewardAsync(ProductConfig productConfig)
    {
        if (!await EnsureInitializedAsync())
        {
            return PurchaseResult.Failed("Firebase hazir degil");
        }

        if (productConfig == null || string.IsNullOrEmpty(productConfig.Id))
        {
            return PurchaseResult.Failed("Gecersiz urun");
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_ClaimSlotProductReward(BuildProductPayloadJson(productConfig)),
            "WebGL slot product reward claim");

        WebBridgePurchaseResult result = FromJson<WebBridgePurchaseResult>(json);
        if (result == null)
        {
            return PurchaseResult.Failed("Transaction hata");
        }

        return result.isSuccess
            ? PurchaseResult.Success(result.qrPayload)
            : PurchaseResult.Failed(result.error);
    }

    public async Task<Dictionary<string, List<ProductConfig.ProductPrice>>> GetShopPricesAsync()
    {
        if (!await EnsureInitializedAsync())
        {
            return null;
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_GetShopConfig(),
            "WebGL get shop config");

        return ToShopPriceLookup(FromJson<WebBridgeShopConfigResult>(json));
    }

    public async Task<RunStartResult> TryStartRunAsync(int playEnergyCost)
    {
        if (!await EnsureInitializedAsync())
        {
            return RunStartResult.Failed("Firebase hazir degil");
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_StartRun(Mathf.Clamp(playEnergyCost, 0, int.MaxValue)),
            "WebGL start run transaction");

        WebBridgeRunStartResult result = FromJson<WebBridgeRunStartResult>(json);
        if (result == null)
        {
            return RunStartResult.Failed("Transaction hata");
        }

        if (result.isSuccess)
        {
            return RunStartResult.Success(result.runId, result.energyBalance);
        }

        return result.isInsufficient
            ? RunStartResult.Insufficient(result.energyBalance)
            : RunStartResult.Failed(result.error);
    }

    public async Task<RewardClaimResult> ClaimRunRewardsAsync(
        string runId,
        Dictionary<string, int> rewards)
    {
        if (string.IsNullOrEmpty(runId))
        {
            return RewardClaimResult.Failed("Run oturumu bulunamadi");
        }

        if (!await EnsureInitializedAsync())
        {
            return RewardClaimResult.Failed("Firebase hazir degil");
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_ClaimRunRewards(BuildRewardClaimPayloadJson(runId, rewards)),
            "WebGL claim run rewards transaction");

        WebBridgeRewardClaimResult result = FromJson<WebBridgeRewardClaimResult>(json);
        return ToRewardClaimResult(result);
    }

    public async Task<SpinRewardClaimResult> ClaimSpinRewardAsync(int segmentCount)
    {
        if (!await EnsureInitializedAsync())
        {
            return SpinRewardClaimResult.Failed("Firebase hazir degil");
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_ClaimSpinReward(Mathf.Clamp(segmentCount, 0, int.MaxValue)),
            "WebGL claim spin reward transaction");

        WebBridgeSpinRewardClaimResult result = FromJson<WebBridgeSpinRewardClaimResult>(json);
        return ToSpinRewardClaimResult(result);
    }

    public async Task SyncCurrencyAmountAsync(string currencyId, int amount)
    {
        if (string.IsNullOrEmpty(currencyId) || !await EnsureInitializedAsync())
        {
            return;
        }

        if (currencyId != "energy" && amount > 0)
        {
            Debug.LogWarning("[Firestore] Positive currency sync is blocked on the client. currency=" + currencyId);
            return;
        }

        await WaitForOperationJsonAsync(
            RocFirebase_SyncCurrencyAmount(currencyId, Mathf.Clamp(amount, 0, int.MaxValue)),
            "WebGL sync currency amount");
    }

    public async Task<CurrencyMutationResult> ModifyCurrencyAmountAsync(string currencyId, int delta)
    {
        if (string.IsNullOrEmpty(currencyId))
        {
            return CurrencyMutationResult.Failed("Gecersiz currency");
        }

        if (delta > 0)
        {
            return CurrencyMutationResult.Failed("Pozitif currency server reward claim ile verilir");
        }

        if (!await EnsureInitializedAsync())
        {
            return CurrencyMutationResult.Failed("Firebase hazir degil");
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_ModifyCurrencyAmount(currencyId, delta),
            "WebGL modify currency amount");

        return ToCurrencyMutationResult(FromJson<WebBridgeCurrencyMutationResult>(json));
    }

    public async Task<Dictionary<string, int>> GetCurrencyAmountsAsync()
    {
        if (!await EnsureInitializedAsync())
        {
            return null;
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_GetCurrencyAmounts(),
            "WebGL get currency amounts");

        WebBridgeCurrencyAmountsResult result = FromJson<WebBridgeCurrencyAmountsResult>(json);
        if (result?.currencies == null)
        {
            return null;
        }

        var currencies = new Dictionary<string, int>();
        foreach (WebBridgeCurrencyEntry currency in result.currencies)
        {
            if (currency == null || string.IsNullOrEmpty(currency.id))
            {
                continue;
            }

            currencies[currency.id] = Mathf.Clamp(currency.amount, 0, int.MaxValue);
        }

        return currencies;
    }

    public async Task<CurrencyMutationResult> GetCurrencyAmountAsync(string currencyId)
    {
        if (string.IsNullOrEmpty(currencyId))
        {
            return CurrencyMutationResult.Failed("Gecersiz currency");
        }

        if (!await EnsureInitializedAsync())
        {
            return CurrencyMutationResult.Failed("Firebase hazir degil");
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_GetCurrencyAmount(currencyId),
            "WebGL get currency amount");

        return ToCurrencyMutationResult(FromJson<WebBridgeCurrencyMutationResult>(json));
    }

    public async Task<EnergyTransactionResult> GetEnergyAmountAsync(int maxEnergy)
    {
        if (!await EnsureInitializedAsync())
        {
            return EnergyTransactionResult.Failed("Firebase hazir degil");
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_GetEnergyAmount(maxEnergy),
            "WebGL get energy amount");

        return ToEnergyResult(FromJson<WebBridgeEnergyResult>(json));
    }

    public IDisposable ListenToCurrencyAmount(string currencyId, Action<int> onChanged)
    {
        if (string.IsNullOrEmpty(currencyId) || onChanged == null)
        {
            return null;
        }

        return new WebGlCurrencyAmountListener(this, currencyId, onChanged);
    }

    public async Task<EnergyTransactionResult> ClaimDailyEnergyAsync(
        int maxEnergy,
        int dailyEnergy,
        float cooldownHours)
    {
        if (!await EnsureInitializedAsync())
        {
            return EnergyTransactionResult.Failed("Firebase hazir degil");
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_ClaimDailyEnergy(maxEnergy, dailyEnergy, cooldownHours),
            "WebGL daily energy transaction");

        return ToEnergyResult(FromJson<WebBridgeEnergyResult>(json));
    }

    public async Task<EnergyTransactionResult> TrySpendEnergyAsync(int amount)
    {
        if (!await EnsureInitializedAsync())
        {
            return EnergyTransactionResult.Failed("Firebase hazir degil");
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_TrySpendEnergy(amount),
            "WebGL spend energy transaction");

        return ToEnergyResult(FromJson<WebBridgeEnergyResult>(json));
    }

    public async Task<FreeSpinTransactionResult> TryUseFreeSpinAsync(float cooldownHours)
    {
        if (!await EnsureInitializedAsync())
        {
            return FreeSpinTransactionResult.Failed("Firebase hazir degil");
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_TryUseFreeSpin(cooldownHours),
            "WebGL free spin transaction");

        WebBridgeFreeSpinResult result = FromJson<WebBridgeFreeSpinResult>(json);
        if (result == null)
        {
            return FreeSpinTransactionResult.Failed("Transaction hata");
        }

        if (result.isSuccess)
        {
            return FreeSpinTransactionResult.Success();
        }

        return result.isOnCooldown
            ? FreeSpinTransactionResult.Cooldown()
            : FreeSpinTransactionResult.Failed(result.error);
    }

    public async Task<Dictionary<string, string>> GetActivePurchasedProductsAsync()
    {
        if (!await EnsureInitializedAsync())
        {
            return null;
        }

        string json = await WaitForOperationJsonAsync(
            RocFirebase_GetActivePurchasedProducts(),
            "WebGL get active purchased products");

        WebBridgePurchasedProductsResult result = FromJson<WebBridgePurchasedProductsResult>(json);
        if (result?.products == null)
        {
            return null;
        }

        var products = new Dictionary<string, string>();
        foreach (WebBridgePurchasedProduct product in result.products)
        {
            if (product == null || string.IsNullOrEmpty(product.productId))
            {
                continue;
            }

            products[product.productId] = product.qrPayload ?? string.Empty;
        }

        return products;
    }

    private Task<bool> EnsureInitializedAsync()
    {
        return InitializeServiceAsync();
    }

    private async Task<bool> InitializeAsync()
    {
        IsReady = false;
        RocFirebase_BeginInitialize();

        for (int attempt = 0; ; attempt++)
        {
            int state = RocFirebase_GetInitState();
            if (state == 1)
            {
                string userId = RocFirebase_GetUserId();
                if (!string.IsNullOrEmpty(userId))
                {
                    activeUserId = userId;
                    SaveLocalUserId(activeUserId);
                }

                IsReady = true;
                Debug.Log("[Firestore] WebGL Firebase ready. User document warmup finished. uid=" + activeUserId);
                return true;
            }

            if (state == 2)
            {
                Debug.LogWarning("[Firestore] WebGL Firebase init failed: " + RocFirebase_GetLastError());
                return false;
            }

            if (attempt > 0 && attempt % FirebaseInitProgressLogIntervalAttempts == 0)
            {
                Debug.Log("[Firestore] WebGL Firebase auth state is still resolving; continuing to wait.");
            }

            await UniTask.Delay(PollDelayMs);
        }
    }

    private static async Task<string> WaitForOperationJsonAsync(int operationId, string operationName)
    {
        if (operationId <= 0)
        {
            Debug.LogWarning("[Firestore] " + operationName + " could not start.");
            return null;
        }

        try
        {
            for (;;)
            {
                int state = RocFirebase_GetOperationState(operationId);
                if (state == 1)
                {
                    return RocFirebase_GetOperationResultJson(operationId);
                }

                if (state == 2)
                {
                    Debug.LogWarning("[Firestore] " + operationName + " failed: " + RocFirebase_GetOperationError(operationId));
                    return null;
                }

                await UniTask.Delay(PollDelayMs);
            }
        }
        finally
        {
            RocFirebase_ReleaseOperation(operationId);
        }
    }

    private static T FromJson<T>(string json) where T : class
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Firestore] WebGL bridge JSON parse failed: " + e.Message);
            return null;
        }
    }

    private static EnergyTransactionResult ToEnergyResult(WebBridgeEnergyResult result)
    {
        if (result == null)
        {
            return EnergyTransactionResult.Failed("Transaction hata");
        }

        if (result.isSuccess)
        {
            return EnergyTransactionResult.Success(result.balance, result.wasRefilled);
        }

        return result.isInsufficient
            ? EnergyTransactionResult.Insufficient(result.balance)
            : EnergyTransactionResult.Failed(result.error);
    }

    private static CurrencyMutationResult ToCurrencyMutationResult(WebBridgeCurrencyMutationResult result)
    {
        if (result == null)
        {
            return CurrencyMutationResult.Failed("Transaction hata");
        }

        if (result.isSuccess)
        {
            return CurrencyMutationResult.Success(result.balance);
        }

        return result.isInsufficient
            ? CurrencyMutationResult.Insufficient(result.balance)
            : CurrencyMutationResult.Failed(result.error);
    }

    private static RewardClaimResult ToRewardClaimResult(WebBridgeRewardClaimResult result)
    {
        if (result == null)
        {
            return RewardClaimResult.Failed("Transaction hata");
        }

        if (!result.isSuccess)
        {
            return RewardClaimResult.Failed(result.error);
        }

        var grants = new Dictionary<string, int>();
        if (result.grants != null)
        {
            foreach (WebBridgeRewardGrant grant in result.grants)
            {
                if (grant == null ||
                    string.IsNullOrEmpty(grant.currencyId) ||
                    grant.grantedAmount <= 0)
                {
                    continue;
                }

                grants.TryGetValue(grant.currencyId, out int currentAmount);
                grants[grant.currencyId] = Mathf.Clamp(currentAmount + grant.grantedAmount, 0, int.MaxValue);
            }
        }

        return RewardClaimResult.Success(grants, result.durationSeconds);
    }

    private static SpinRewardClaimResult ToSpinRewardClaimResult(WebBridgeSpinRewardClaimResult result)
    {
        if (result == null)
        {
            return SpinRewardClaimResult.Failed("Transaction hata");
        }

        if (result.isSuccess)
        {
            return SpinRewardClaimResult.Success(
                result.currencyId,
                result.amount,
                result.balance,
                result.energyBalance,
                result.segmentIndex,
                result.rewardId);
        }

        return result.isInsufficient
            ? SpinRewardClaimResult.Insufficient(result.energyBalance)
            : SpinRewardClaimResult.Failed(result.error);
    }

    private static Dictionary<string, List<ProductConfig.ProductPrice>> ToShopPriceLookup(
        WebBridgeShopConfigResult result)
    {
        if (result == null || !result.isSuccess || result.products == null)
        {
            return null;
        }

        var lookup = new Dictionary<string, List<ProductConfig.ProductPrice>>();
        foreach (WebBridgeShopProduct product in result.products)
        {
            if (product == null || string.IsNullOrEmpty(product.productId) || product.prices == null)
            {
                continue;
            }

            var prices = new List<ProductConfig.ProductPrice>();
            foreach (WebBridgeProductPrice price in product.prices)
            {
                if (price == null || string.IsNullOrEmpty(price.currency) || price.amount <= 0)
                {
                    continue;
                }

                prices.Add(new ProductConfig.ProductPrice
                {
                    currency = price.currency,
                    amount = Mathf.Clamp(price.amount, 0, int.MaxValue)
                });
            }

            if (prices.Count > 0)
            {
                lookup[product.productId] = prices;
            }
        }

        return lookup;
    }

    private static string BuildProductPayloadJson(ProductConfig productConfig)
    {
        var payload = new WebBridgeProductPayload
        {
            productId = productConfig != null ? productConfig.Id : string.Empty,
            productName = productConfig != null ? productConfig.Name : string.Empty,
            productDescription = productConfig != null ? productConfig.Description : string.Empty,
            section = productConfig != null ? productConfig.section.ToString() : string.Empty,
            prices = Array.Empty<WebBridgeProductPrice>()
        };

        if (productConfig?.Prices != null && productConfig.Prices.Count > 0)
        {
            var prices = new List<WebBridgeProductPrice>();
            foreach (ProductConfig.ProductPrice price in productConfig.Prices)
            {
                if (price == null || string.IsNullOrEmpty(price.currency) || price.amount <= 0)
                {
                    continue;
                }

                prices.Add(new WebBridgeProductPrice
                {
                    currency = price.currency,
                    amount = Mathf.Clamp(price.amount, 0, int.MaxValue)
                });
            }

            payload.prices = prices.ToArray();
        }

        return JsonUtility.ToJson(payload);
    }

    private static string BuildRewardClaimPayloadJson(string runId, Dictionary<string, int> rewards)
    {
        var payload = new WebBridgeRewardClaimPayload
        {
            runId = runId ?? string.Empty,
            rewards = Array.Empty<WebBridgeRewardEntry>()
        };

        if (rewards != null && rewards.Count > 0)
        {
            var entries = new List<WebBridgeRewardEntry>();
            foreach (KeyValuePair<string, int> reward in rewards)
            {
                if (string.IsNullOrEmpty(reward.Key) || reward.Value <= 0)
                {
                    continue;
                }

                entries.Add(new WebBridgeRewardEntry
                {
                    currencyId = reward.Key,
                    amount = reward.Value
                });
            }

            payload.rewards = entries.ToArray();
        }

        return JsonUtility.ToJson(payload);
    }

    private static void SaveLocalUserId(string userId)
    {
        PlayerPrefs.SetString(LocalUserIdPrefsKey, userId);
        PlayerPrefs.Save();
    }

    private sealed class WebGlCurrencyAmountListener : IDisposable
    {
        private const int PollIntervalMs = 2000;

        private readonly FirestoreGameSecurityService service;
        private readonly string currencyId;
        private readonly Action<int> onChanged;
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private int? lastAmount;

        public WebGlCurrencyAmountListener(
            FirestoreGameSecurityService service,
            string currencyId,
            Action<int> onChanged)
        {
            this.service = service;
            this.currencyId = currencyId;
            this.onChanged = onChanged;
            _ = PollAsync(cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        private async Task PollAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Dictionary<string, int> currencies = await service.GetCurrencyAmountsAsync();
                    if (currencies != null &&
                        currencies.TryGetValue(currencyId, out int amount) &&
                        (!lastAmount.HasValue || lastAmount.Value != amount))
                    {
                        lastAmount = amount;
                        onChanged(amount);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Firestore] WebGL currency listener skipped: " + e.Message);
                }

                try
                {
                    await UniTask.Delay(PollIntervalMs, cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    [Serializable]
    private class WebBridgeUserResult
    {
        public string userId;
    }

    [Serializable]
    private class WebBridgeProductPayload
    {
        public string productId;
        public string productName;
        public string productDescription;
        public string section;
        public WebBridgeProductPrice[] prices;
    }

    [Serializable]
    private class WebBridgeProductPrice
    {
        public string currency;
        public int amount;
    }

    [Serializable]
    private class WebBridgePurchaseResult
    {
        public bool isSuccess;
        public string qrPayload;
        public string error;
    }

    [Serializable]
    private class WebBridgeShopConfigResult
    {
        public bool isSuccess;
        public WebBridgeShopProduct[] products;
        public string error;
    }

    [Serializable]
    private class WebBridgeShopProduct
    {
        public string productId;
        public WebBridgeProductPrice[] prices;
    }

    [Serializable]
    private class WebBridgeCurrencyAmountsResult
    {
        public WebBridgeCurrencyEntry[] currencies;
    }

    [Serializable]
    private class WebBridgeCurrencyEntry
    {
        public string id;
        public int amount;
    }

    [Serializable]
    private class WebBridgeCurrencyMutationResult
    {
        public bool isSuccess;
        public bool isInsufficient;
        public int balance;
        public string error;
    }

    [Serializable]
    private class WebBridgeEnergyResult
    {
        public bool isSuccess;
        public bool isInsufficient;
        public bool wasRefilled;
        public int balance;
        public string error;
    }

    [Serializable]
    private class WebBridgeFreeSpinResult
    {
        public bool isSuccess;
        public bool isOnCooldown;
        public string error;
    }

    [Serializable]
    private class WebBridgeRunStartResult
    {
        public bool isSuccess;
        public bool isInsufficient;
        public string runId;
        public int energyBalance;
        public string error;
    }

    [Serializable]
    private class WebBridgeRewardClaimPayload
    {
        public string runId;
        public WebBridgeRewardEntry[] rewards;
    }

    [Serializable]
    private class WebBridgeRewardEntry
    {
        public string currencyId;
        public int amount;
    }

    [Serializable]
    private class WebBridgeRewardClaimResult
    {
        public bool isSuccess;
        public string runId;
        public float durationSeconds;
        public WebBridgeRewardGrant[] grants;
        public string error;
    }

    [Serializable]
    private class WebBridgeRewardGrant
    {
        public string currencyId;
        public int requestedAmount;
        public int grantedAmount;
        public int balance;
        public int capPerSecond;
        public int maxAllowed;
    }

    [Serializable]
    private class WebBridgeSpinRewardClaimResult
    {
        public bool isSuccess;
        public bool isInsufficient;
        public int energyBalance;
        public int segmentIndex;
        public string rewardId;
        public string currencyId;
        public int amount;
        public int balance;
        public string error;
    }

    [Serializable]
    private class WebBridgePurchasedProductsResult
    {
        public WebBridgePurchasedProduct[] products;
    }

    [Serializable]
    private class WebBridgePurchasedProduct
    {
        public string productId;
        public string qrPayload;
    }
}
#else
public class FirestoreGameSecurityService
{
    private const string UsersCollection = "users";
    private const string CurrenciesCollection = "currencies";
    private const string EntitlementsCollection = "entitlements";
    private const string DailySpinDocument = "dailySpin";
    private const string EnergyCurrencyId = "energy";
    private const string QrCodesCollection = "QRCodes";
    private const string PurchasedProductsCollection = "purchasedProducts";
    private const string RunSessionsCollection = "runSessions";
    private const string CurrencyEventsCollection = "currencyEvents";
    private const string LocalUserIdPrefsKey = "FirestoreGameSecurityService.UserId";
    private const int InitialEnergyAmount = 15;
    private const int MaxCoinPerSecond = 2;
    private const int DefaultSpinEnergyCost = 1;
    private const string FunctionsRegion = "us-central1";
    private const string FunctionsProjectIdFallback = "matcha-birdy";
    private const int FirebaseInitMaxRetryCount = 4;
    private const int AuthOperationMaxRetryCount = 3;
    private const int AuthTokenRefreshMaxRetryCount = 3;
    private const int FirestoreOperationMaxRetryCount = 2;
    private const int RetryDelayMs = 300;

    private static readonly HttpClient FunctionsHttpClient = new HttpClient();

    public static FirestoreGameSecurityService Instance { get; private set; }

    public bool IsReady { get; private set; }

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string activeUserId;
    private const int EnsureUserDocMaxRetryCount = 3;
    private readonly IAudioService audioService;
    private readonly SoundData purchaseSound;
    private Task<bool> initializationTask;

    public FirestoreGameSecurityService()
        : this(null, null)
    {
    }

    public FirestoreGameSecurityService(IAudioService audioService, SoundData purchaseSound)
    {
        Instance = this;
        this.audioService = audioService;
        this.purchaseSound = purchaseSound;
    }

    public Task<bool> InitializeServiceAsync()
    {
        if (IsReady)
        {
            return Task.FromResult(true);
        }

        if (initializationTask == null || initializationTask.IsCompleted)
        {
            initializationTask = InitializeAsync();
        }

        return initializationTask;
    }

    private async Task<bool> InitializeAsync()
    {
        IsReady = false;
        Exception lastException = null;

        for (int attempt = 1; attempt <= FirebaseInitMaxRetryCount; attempt++)
        {
            try
            {
                DependencyStatus dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dependencyStatus != DependencyStatus.Available)
                {
                    throw new InvalidOperationException("Firebase dependencies are not available: " + dependencyStatus);
                }

                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;

                activeUserId = await ResolveActiveUserIdAsync();
                bool authTokenReady = await WarmupAuthSessionAsync();
                if (!authTokenReady)
                {
                    throw new InvalidOperationException("Firebase auth token could not be refreshed.");
                }

                if (auth?.CurrentUser == null || db == null || string.IsNullOrEmpty(activeUserId))
                {
                    throw new InvalidOperationException("Firebase initialized without a usable auth/firestore session.");
                }

                await EnsureUserDocumentAsync(GetUserId());
                IsReady = true;
                Debug.Log("✅ Firebase ready. User document warmup finished.");
                return true;
            }
            catch (Exception e)
            {
                lastException = e;
                IsReady = false;

                Debug.LogWarning(
                    $"[Firestore] Firebase init attempt {attempt}/{FirebaseInitMaxRetryCount} failed: {GetExceptionSummary(e)}");

                if (attempt < FirebaseInitMaxRetryCount)
                {
                    await UniTask.Delay(GetRetryDelayMs(attempt));
                }
            }
        }

        Debug.LogWarning(
            "[Firestore] Firebase could not be initialized after retries. " +
            "Firebase-backed startup will report false. Last issue: " +
            GetExceptionSummary(lastException));
        return false;
    }

    public string GetUserId()
    {
        return activeUserId;
    }

    private async Task<string> ResolveActiveUserIdAsync()
    {
        FirebaseUser currentUser = auth.CurrentUser;

        if (currentUser == null)
        {
#if UNITY_EDITOR
            Debug.Log("ℹ️ Firebase current user not found. Trying anonymous sign-in...");
            currentUser = await SignInAnonymouslyWithRetryAsync("initial auth");
#else
            throw new InvalidOperationException(
                "Google Firebase auth is required outside the Unity Editor. " +
                "WebGL uses the Firebase Web redirect flow; native builds need a Google sign-in token provider.");
#endif
        }

        if (currentUser == null || string.IsNullOrEmpty(currentUser.UserId))
        {
            throw new InvalidOperationException("Firebase auth did not return a valid user.");
        }

#if !UNITY_EDITOR
        if (!IsGoogleUser(currentUser))
        {
            throw new InvalidOperationException(
                "Firebase current user is not a Google account. Sign out and complete Google sign-in first.");
        }
#endif

        Debug.Log("✅ Firebase auth user ready: " + currentUser.UserId);

        string savedUserId = PlayerPrefs.GetString(LocalUserIdPrefsKey, string.Empty);
        string currentUserId = currentUser.UserId;

        if (string.IsNullOrEmpty(savedUserId))
        {
            SaveLocalUserId(currentUserId);
            return currentUserId;
        }

        if (savedUserId == currentUserId)
        {
            return savedUserId;
        }

        Debug.LogWarning(
            $"⚠️ Saved user id ({savedUserId}) and Firebase auth user ({currentUserId}) mismatch. Continuing with Firebase current user and updating local cache.");

        SaveLocalUserId(currentUserId);
        return currentUserId;
    }

    private async Task<bool> EnsureFreshAuthTokenAsync(bool forceRefresh = false)
    {
        if (auth?.CurrentUser == null)
        {
            Debug.LogWarning("[Firestore] Auth token refresh skipped because CurrentUser is null.");
            return false;
        }

        Exception lastException = null;

        for (int attempt = 1; attempt <= AuthTokenRefreshMaxRetryCount; attempt++)
        {
            FirebaseUser currentUser = auth.CurrentUser;
            if (currentUser == null)
            {
                Debug.LogWarning("[Firestore] Auth token refresh stopped because CurrentUser became null.");
                return false;
            }

            try
            {
                string token = await currentUser.TokenAsync(forceRefresh);
                if (!string.IsNullOrEmpty(token))
                {
                    Debug.Log($"✅ Firebase auth token ready for uid: {currentUser.UserId}");
                    return true;
                }

                lastException = new InvalidOperationException("Auth token refresh returned an empty token.");
            }
            catch (Exception e)
            {
                lastException = e;
                Debug.LogWarning(
                    $"[Firestore] Auth token refresh attempt {attempt}/{AuthTokenRefreshMaxRetryCount} failed: {GetExceptionSummary(e)}");
            }

            if (attempt < AuthTokenRefreshMaxRetryCount)
            {
                await UniTask.Delay(GetRetryDelayMs(attempt));
            }
        }

        Debug.LogWarning("[Firestore] Auth token refresh skipped after retries: " + GetExceptionSummary(lastException));
        return false;
    }

    private async Task<bool> WarmupAuthSessionAsync()
    {
        if (await EnsureFreshAuthTokenAsync(false))
        {
            return true;
        }

        if (await EnsureFreshAuthTokenAsync(true))
        {
            return true;
        }

        if (auth?.CurrentUser == null)
        {
            throw new InvalidOperationException("Auth warmup failed because CurrentUser is null.");
        }

        Debug.LogWarning("[Firestore] Firebase auth token is not warm; initialization will wait for a clear Firebase result.");
        return false;
    }

#if UNITY_EDITOR
    private async Task<FirebaseUser> SignInAnonymouslyWithRetryAsync(string reason)
    {
        Exception lastException = null;

        for (int attempt = 1; attempt <= AuthOperationMaxRetryCount; attempt++)
        {
            try
            {
                var authResult = await auth.SignInAnonymouslyAsync();
                if (authResult?.User != null && !string.IsNullOrEmpty(authResult.User.UserId))
                {
                    Debug.Log($"✅ Firebase anonymous sign-in succeeded ({reason}) for uid: {authResult.User.UserId}");
                    return authResult.User;
                }

                lastException = new InvalidOperationException("Firebase anonymous sign-in returned an empty user.");
            }
            catch (Exception e)
            {
                lastException = e;
                Debug.LogWarning(
                    $"[Firestore] Anonymous sign-in attempt {attempt}/{AuthOperationMaxRetryCount} failed ({reason}): {GetExceptionSummary(e)}");
            }

            if (attempt < AuthOperationMaxRetryCount)
            {
                await UniTask.Delay(GetRetryDelayMs(attempt));
            }
        }

        throw new InvalidOperationException(
            "Firebase anonymous sign-in failed after retries. " +
            "Firebase Console > Authentication > Sign-in method ekranında Anonymous provider'ı etkin olmalı.",
            lastException);
    }
#endif

    private static int GetRetryDelayMs(int attempt)
    {
        return RetryDelayMs * Math.Max(1, attempt);
    }

    private static string GetExceptionSummary(Exception exception)
    {
        if (exception == null)
        {
            return "unknown";
        }

        if (exception is AggregateException aggregateException)
        {
            exception = aggregateException.Flatten().InnerException ?? exception;
        }

        return exception.GetType().Name + ": " + exception.Message;
    }

    private static bool IsGoogleUser(FirebaseUser user)
    {
        if (user == null || user.ProviderData == null)
        {
            return false;
        }

        foreach (var provider in user.ProviderData)
        {
            if (provider != null &&
                string.Equals(provider.ProviderId, "google.com", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<T> ExecuteFirestoreOperationWithRetryAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        Func<Exception, T> fallbackFactory)
    {
        Exception lastException = null;

        for (int attempt = 1; attempt <= FirestoreOperationMaxRetryCount; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception e)
            {
                lastException = e;
                Debug.LogWarning(
                    $"[Firestore] {operationName} attempt {attempt}/{FirestoreOperationMaxRetryCount} failed: {GetExceptionSummary(e)}");

                if (attempt < FirestoreOperationMaxRetryCount)
                {
                    await EnsureFreshAuthTokenAsync(false);
                    await UniTask.Delay(GetRetryDelayMs(attempt));
                }
            }
        }

        return fallbackFactory != null ? fallbackFactory(lastException) : default;
    }

    private static bool UseCloudFunctionsForEconomy()
    {
        return true;
    }

    private Task<RunStartResult> CallStartRunFunctionAsync(int playEnergyCost)
    {
        var payload = new Dictionary<string, object>
        {
            { "playEnergyCost", Mathf.Clamp(playEnergyCost, 0, int.MaxValue) }
        };

        return CallCallableFunctionAsync(
            "startRun",
            payload,
            ParseRunStartFunctionResult,
            RunStartResult.Failed);
    }

    private Task<RewardClaimResult> CallRunRewardFunctionAsync(
        string runId,
        Dictionary<string, int> rewards)
    {
        if (string.IsNullOrEmpty(runId))
        {
            return Task.FromResult(RewardClaimResult.Failed("Run oturumu bulunamadi"));
        }

        var entries = new List<Dictionary<string, object>>();
        foreach (KeyValuePair<string, int> reward in SanitizeRewards(rewards))
        {
            entries.Add(new Dictionary<string, object>
            {
                { "currencyId", reward.Key },
                { "amount", Mathf.Clamp(reward.Value, 0, int.MaxValue) }
            });
        }

        var payload = new Dictionary<string, object>
        {
            { "runId", runId },
            { "rewards", entries }
        };

        return CallCallableFunctionAsync(
            "claimRunRewards",
            payload,
            ParseRunRewardFunctionResult,
            RewardClaimResult.Failed);
    }

    private Task<SpinRewardClaimResult> CallSpinRewardFunctionAsync(int segmentCount)
    {
        var payload = new Dictionary<string, object>
        {
            { "segmentCount", Mathf.Clamp(segmentCount, 0, int.MaxValue) }
        };

        return CallCallableFunctionAsync(
            "claimSpinReward",
            payload,
            ParseSpinRewardFunctionResult,
            SpinRewardClaimResult.Failed);
    }

    private Task<PurchaseResult> CallPurchaseProductFunctionAsync(string productId)
    {
        var payload = new Dictionary<string, object>
        {
            { "productId", productId ?? string.Empty }
        };

        return CallCallableFunctionAsync(
            "purchaseProduct",
            payload,
            ParsePurchaseFunctionResult,
            PurchaseResult.Failed);
    }

    private Task<Dictionary<string, List<ProductConfig.ProductPrice>>> CallShopConfigFunctionAsync()
    {
        return CallCallableFunctionAsync(
            "getShopConfig",
            new Dictionary<string, object>(),
            ParseShopConfigFunctionResult,
            _ => null);
    }

    private async Task<T> CallCallableFunctionAsync<T>(
        string functionName,
        object payload,
        Func<JObject, T> resultParser,
        Func<string, T> failureFactory)
    {
        if (!IsReady || auth == null || auth.CurrentUser == null)
        {
            return failureFactory("Firebase auth hazir degil");
        }

        try
        {
            string token = await auth.CurrentUser.TokenAsync(false);
            if (string.IsNullOrEmpty(token))
            {
                token = await auth.CurrentUser.TokenAsync(true);
            }

            if (string.IsNullOrEmpty(token))
            {
                return failureFactory("Firebase auth token alinamadi");
            }

            string requestJson = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "data", payload ?? new Dictionary<string, object>() }
            });

            using (var request = new HttpRequestMessage(
                HttpMethod.Post,
                BuildCallableFunctionUrl(functionName)))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await FunctionsHttpClient.SendAsync(request))
                {
                    string responseJson = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        string responseSummary = string.IsNullOrEmpty(responseJson)
                            ? response.ReasonPhrase
                            : responseJson.Substring(0, Math.Min(200, responseJson.Length));

                        if (responseSummary.TrimStart().StartsWith("<"))
                        {
                            responseSummary = response.StatusCode == System.Net.HttpStatusCode.NotFound
                                ? "Cloud Function endpoint bulunamadi. Deploy gerekli: firebase deploy --only functions"
                                : response.ReasonPhrase;
                        }

                        return failureFactory("Function HTTP " + (int)response.StatusCode + ": " + responseSummary);
                    }

                    JObject responseObject = string.IsNullOrEmpty(responseJson)
                        ? new JObject()
                        : JObject.Parse(responseJson);

                    JToken errorToken = responseObject["error"];
                    if (errorToken != null)
                    {
                        return failureFactory(ReadString(errorToken, "message", errorToken.ToString()));
                    }

                    JObject resultObject = responseObject["result"] as JObject;
                    if (resultObject == null)
                    {
                        resultObject = responseObject["data"] as JObject ?? responseObject;
                    }

                    return resultParser(resultObject);
                }
            }
        }
        catch (Exception e)
        {
            return failureFactory(GetExceptionSummary(e));
        }
    }

    private static string BuildCallableFunctionUrl(string functionName)
    {
        return "https://" + FunctionsRegion + "-" + ResolveFunctionsProjectId() +
               ".cloudfunctions.net/" + functionName;
    }

    private static string ResolveFunctionsProjectId()
    {
        try
        {
            string projectId = FirebaseApp.DefaultInstance?.Options?.ProjectId;
            if (!string.IsNullOrEmpty(projectId))
            {
                return projectId;
            }
        }
        catch
        {
            // Fall back to the WebGL project id used by this build.
        }

        return FunctionsProjectIdFallback;
    }

    private static RunStartResult ParseRunStartFunctionResult(JObject result)
    {
        if (ReadBool(result, "isSuccess"))
        {
            return RunStartResult.Success(
                ReadString(result, "runId"),
                ReadInt(result, "energyBalance"));
        }

        return ReadBool(result, "isInsufficient")
            ? RunStartResult.Insufficient(ReadInt(result, "energyBalance"))
            : RunStartResult.Failed(ReadString(result, "error", "Run transaction hata"));
    }

    private static RewardClaimResult ParseRunRewardFunctionResult(JObject result)
    {
        if (!ReadBool(result, "isSuccess"))
        {
            return RewardClaimResult.Failed(ReadString(result, "error", "Reward claim hata"));
        }

        var grants = new Dictionary<string, int>();
        JArray grantsArray = result["grants"] as JArray;
        if (grantsArray != null)
        {
            foreach (JToken grantToken in grantsArray)
            {
                if (!(grantToken is JObject grantObject))
                {
                    continue;
                }

                string currencyId = ReadString(grantObject, "currencyId");
                int grantedAmount = ReadInt(grantObject, "grantedAmount");
                if (string.IsNullOrEmpty(currencyId) || grantedAmount <= 0)
                {
                    continue;
                }

                grants.TryGetValue(currencyId, out int currentAmount);
                grants[currencyId] = Mathf.Clamp(currentAmount + grantedAmount, 0, int.MaxValue);
            }
        }

        return RewardClaimResult.Success(grants, ReadFloat(result, "durationSeconds"));
    }

    private static SpinRewardClaimResult ParseSpinRewardFunctionResult(JObject result)
    {
        if (ReadBool(result, "isSuccess"))
        {
            return SpinRewardClaimResult.Success(
                ReadString(result, "currencyId"),
                ReadInt(result, "amount"),
                ReadInt(result, "balance"),
                ReadInt(result, "energyBalance"),
                ReadInt(result, "segmentIndex"),
                ReadString(result, "rewardId"));
        }

        return ReadBool(result, "isInsufficient")
            ? SpinRewardClaimResult.Insufficient(ReadInt(result, "energyBalance"))
            : SpinRewardClaimResult.Failed(ReadString(result, "error", "Spin reward claim hata"));
    }

    private static PurchaseResult ParsePurchaseFunctionResult(JObject result)
    {
        if (ReadBool(result, "isSuccess"))
        {
            return PurchaseResult.Success(ReadString(result, "qrPayload"));
        }

        return PurchaseResult.Failed(ReadString(result, "error", "Purchase transaction hata"));
    }

    private static Dictionary<string, List<ProductConfig.ProductPrice>> ParseShopConfigFunctionResult(JObject result)
    {
        if (!ReadBool(result, "isSuccess"))
        {
            return null;
        }

        JArray productsArray = result["products"] as JArray;
        if (productsArray == null)
        {
            return null;
        }

        var lookup = new Dictionary<string, List<ProductConfig.ProductPrice>>();
        foreach (JToken productToken in productsArray)
        {
            if (!(productToken is JObject productObject))
            {
                continue;
            }

            string productId = ReadString(productObject, "productId");
            if (string.IsNullOrEmpty(productId))
            {
                continue;
            }

            JArray pricesArray = productObject["prices"] as JArray;
            if (pricesArray == null)
            {
                continue;
            }

            var prices = new List<ProductConfig.ProductPrice>();
            foreach (JToken priceToken in pricesArray)
            {
                if (!(priceToken is JObject priceObject))
                {
                    continue;
                }

                string currency = ReadString(priceObject, "currency");
                int amount = ReadInt(priceObject, "amount");
                if (string.IsNullOrEmpty(currency) || amount <= 0)
                {
                    continue;
                }

                prices.Add(new ProductConfig.ProductPrice
                {
                    currency = currency,
                    amount = amount
                });
            }

            if (prices.Count > 0)
            {
                lookup[productId] = prices;
            }
        }

        return lookup;
    }

    private static string ReadString(JToken token, string fieldName, string fallback = "")
    {
        if (token == null)
        {
            return fallback;
        }

        JToken value = token[fieldName];
        return value != null ? value.ToString() : fallback;
    }

    private static int ReadInt(JToken token, string fieldName, int fallback = 0)
    {
        if (token == null)
        {
            return fallback;
        }

        JToken value = token[fieldName];
        return value != null && int.TryParse(value.ToString(), out int parsed)
            ? Mathf.Clamp(parsed, 0, int.MaxValue)
            : fallback;
    }

    private static float ReadFloat(JToken token, string fieldName, float fallback = 0f)
    {
        if (token == null)
        {
            return fallback;
        }

        JToken value = token[fieldName];
        return value != null &&
               float.TryParse(
                   value.ToString(),
                   NumberStyles.Float,
                   CultureInfo.InvariantCulture,
                   out float parsed)
            ? Mathf.Max(0f, parsed)
            : fallback;
    }

    private static bool ReadBool(JToken token, string fieldName)
    {
        if (token == null)
        {
            return false;
        }

        JToken value = token[fieldName];
        return value != null && bool.TryParse(value.ToString(), out bool parsed) && parsed;
    }

    private static void SaveLocalUserId(string userId)
    {
        PlayerPrefs.SetString(LocalUserIdPrefsKey, userId);
        PlayerPrefs.Save();
    }

    public async Task ClearCurrentUserDataAsync()
    {
        if (!IsReady)
        {
            return;
        }

        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        try
        {
            DocumentReference userRef = db.Collection(UsersCollection).Document(userId);

            await DeleteCollectionDocumentsAsync(userRef.Collection(CurrenciesCollection));
            await DeleteCollectionDocumentsAsync(userRef.Collection(QrCodesCollection));
            await DeleteCollectionDocumentsAsync(userRef.Collection(PurchasedProductsCollection));

            await userRef.DeleteAsync();

            PlayerPrefs.DeleteKey(LocalUserIdPrefsKey);
            PlayerPrefs.Save();

            auth.SignOut();

            activeUserId = await ResolveActiveUserIdAsync();
            if (!await WarmupAuthSessionAsync())
            {
                return;
            }

            await EnsureUserDocumentAsync(activeUserId);

            Debug.Log("✅ Current user data cleared and Firebase auth is ready: " + activeUserId);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Firestore] ClearCurrentUserDataAsync skipped: " + GetExceptionSummary(e));
        }
    }

    private static async Task DeleteCollectionDocumentsAsync(CollectionReference collectionReference)
    {
        QuerySnapshot snapshot = await collectionReference.GetSnapshotAsync();

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            await document.Reference.DeleteAsync();
        }
    }

    // --------------------------------------------------
    // USER DOC
    // --------------------------------------------------
    public async Task EnsureUserDocumentAsync(string userId)
    {
        if (db == null || string.IsNullOrEmpty(userId))
            return;

        if (auth == null || auth.CurrentUser == null)
        {
            Debug.LogWarning("[Firestore] EnsureUserDocumentAsync skipped: Auth current user is null.");
            return;
        }

        if (auth.CurrentUser.UserId != userId)
        {
            Debug.LogWarning($"⚠️ EnsureUserDocumentAsync userId mismatch. auth: {auth.CurrentUser.UserId}, requested: {userId}");
        }

        DocumentReference userRef =
            db.Collection(UsersCollection).Document(userId);

        Exception lastException = null;

        for (int attempt = 1; attempt <= EnsureUserDocMaxRetryCount; attempt++)
        {
            try
            {
                Dictionary<string, object> userPayload = new Dictionary<string, object>
                {
                    { "userId", userId },
                    { "lastSeenAt", FieldValue.ServerTimestamp }
                };

                DocumentSnapshot userSnapshot = await userRef.GetSnapshotAsync();
                if (!userSnapshot.Exists)
                {
                    userPayload["createdAt"] = FieldValue.ServerTimestamp;
                }

                await userRef.SetAsync(userPayload, SetOptions.MergeAll);
                await EnsureInitialEnergyDocumentAsync(userRef);
                Debug.Log($"✅ EnsureUserDocumentAsync success (attempt {attempt}) for uid: {userId}");
                return;
            }
            catch (Exception e)
            {
                lastException = e;
                string currentAuthUserId = auth?.CurrentUser?.UserId ?? "<null>";
                Debug.LogWarning(
                    $"⚠️ EnsureUserDocumentAsync attempt {attempt}/{EnsureUserDocMaxRetryCount} failed. " +
                    $"requestedUid={userId}, authUid={currentAuthUserId}, error={GetExceptionSummary(e)}");

                if (attempt < EnsureUserDocMaxRetryCount)
                {
                    await EnsureFreshAuthTokenAsync(false);
                    await UniTask.Delay(GetRetryDelayMs(attempt));
                }
            }
        }

        Debug.LogWarning(
            $"[Firestore] EnsureUserDocumentAsync skipped after {EnsureUserDocMaxRetryCount} attempts for uid {userId}: " +
            GetExceptionSummary(lastException));
    }

    private async Task EnsureInitialEnergyDocumentAsync(DocumentReference userRef)
    {
        if (db == null || userRef == null)
        {
            return;
        }

        DocumentReference energyRef =
            userRef.Collection(CurrenciesCollection).Document(EnergyCurrencyId);

        await db.RunTransactionAsync(async transaction =>
        {
            DocumentSnapshot energySnapshot =
                await transaction.GetSnapshotAsync(energyRef);

            if (energySnapshot.Exists)
            {
                return true;
            }

            transaction.Set(
                energyRef,
                new Dictionary<string, object>
                {
                    { "amount", InitialEnergyAmount },
                    { "updatedAt", FieldValue.ServerTimestamp }
                });

            return true;
        });
    }

    // --------------------------------------------------
    // PURCHASE TRANSACTION
    // --------------------------------------------------
    public async Task<PurchaseResult> TryPurchaseProductAsync(ProductConfig productConfig)
    {
        if (!IsReady)
            return PurchaseResult.Failed("Firebase hazır değil");

        if (productConfig == null || string.IsNullOrEmpty(productConfig.Id))
        {
            return PurchaseResult.Failed("Geçersiz ürün");
        }

        if (UseCloudFunctionsForEconomy())
        {
            PurchaseResult functionResult = await CallPurchaseProductFunctionAsync(productConfig.Id);
            if (functionResult.IsSuccess && purchaseSound != null)
            {
                audioService?.Play(purchaseSound);
            }

            return functionResult;
        }

        if (productConfig.Prices == null || productConfig.Prices.Count == 0)
        {
            return PurchaseResult.Failed("Geçersiz fiyatlandırma");
        }

        foreach (var price in productConfig.Prices)
        {
            if (price == null || string.IsNullOrEmpty(price.currency) || price.amount <= 0)
            {
                return PurchaseResult.Failed("Geçersiz fiyatlandırma");
            }
        }

        string userId = GetUserId();

        await EnsureUserDocumentAsync(userId);

        string qrId = Guid.NewGuid().ToString("N");
        string qrPayload = BuildQrPayload(userId, productConfig.Id, qrId);

        DocumentReference userRef =
            db.Collection(UsersCollection).Document(userId);

        DocumentReference purchasedRef =
            userRef.Collection(PurchasedProductsCollection)
                   .Document(productConfig.Id);

        DocumentReference qrRef =
            userRef.Collection(QrCodesCollection)
                   .Document(qrId);

        bool? success = await ExecuteFirestoreOperationWithRetryAsync<bool?>(
            "Purchase transaction",
            async () =>
            {
                bool transactionSuccess = await db.RunTransactionAsync(async transaction =>
                {
                    DocumentSnapshot purchasedSnap =
                        await transaction.GetSnapshotAsync(purchasedRef);

                    if (purchasedSnap.Exists)
                        return false;

                    var totalCostByCurrency = new Dictionary<string, int>();

                    foreach (var price in productConfig.Prices)
                    {
                        if (price.amount <= 0 || string.IsNullOrEmpty(price.currency))
                        {
                            return false;
                        }

                        if (totalCostByCurrency.TryGetValue(price.currency, out int existingAmount))
                        {
                            totalCostByCurrency[price.currency] = existingAmount + price.amount;
                        }
                        else
                        {
                            totalCostByCurrency[price.currency] = price.amount;
                        }
                    }

                    List<Dictionary<string, object>> priceDetails =
                        BuildPriceDetails(totalCostByCurrency);
                    string spentSummary = FormatPriceSummary(totalCostByCurrency);

                    var balanceByCurrency = new Dictionary<string, int>();

                    // CHECK BALANCE
                    foreach (var currencyCost in totalCostByCurrency)
                    {
                        if (currencyCost.Value <= 0)
                        {
                            return false;
                        }

                        var currencyRef =
                            userRef.Collection(CurrenciesCollection)
                                   .Document(currencyCost.Key);

                        var currencySnap =
                            await transaction.GetSnapshotAsync(currencyRef);

                        int currentBalance = 0;

                        if (currencySnap.Exists &&
                            currencySnap.TryGetValue("amount", out long amount))
                        {
                            currentBalance = Convert.ToInt32(Math.Max(0L, Math.Min(int.MaxValue, amount)));
                        }

                        if (currentBalance < currencyCost.Value)
                            return false;

                        balanceByCurrency[currencyCost.Key] = currentBalance;
                    }

                    // DEDUCT BALANCE
                    foreach (var currencyCost in totalCostByCurrency)
                    {
                        var currencyRef =
                            userRef.Collection(CurrenciesCollection)
                                   .Document(currencyCost.Key);

                        int currentBalance = balanceByCurrency[currencyCost.Key];

                        transaction.Set(currencyRef,
                            new Dictionary<string, object>
                            {
                                { "amount", Math.Max(0, currentBalance - currencyCost.Value) },
                                { "updatedAt", FieldValue.ServerTimestamp }
                            },
                            SetOptions.MergeAll);
                    }

                    // PURCHASE DOC
                    transaction.Set(purchasedRef,
                        new Dictionary<string, object>
                        {
                            { "productId", productConfig.Id },
                            { "productName", productConfig.Name ?? string.Empty },
                            { "productDescription", productConfig.Description ?? string.Empty },
                            { "section", productConfig.section.ToString() },
                            { "qrId", qrId },
                            { "qrPayload", qrPayload },
                            { "prices", priceDetails },
                            { "spentSummary", spentSummary },
                            { "createdAt", FieldValue.ServerTimestamp },
                            { "status", "purchased" }
                        });

                    // QR DOC
                    transaction.Set(qrRef,
                        new Dictionary<string, object>
                        {
                            { "id", qrId },
                            { "productId", productConfig.Id },
                            { "productName", productConfig.Name ?? string.Empty },
                            { "productDescription", productConfig.Description ?? string.Empty },
                            { "section", productConfig.section.ToString() },
                            { "userId", userId },
                            { "payload", qrPayload },
                            { "prices", priceDetails },
                            { "spentSummary", spentSummary },
                            { "createdAt", FieldValue.ServerTimestamp },
                            { "status", "active" },
                            { "source", "store_purchase" }
                        });

                    return true;
                });

                return transactionSuccess;
            },
            _ => null);

        if (!success.HasValue)
        {
            return PurchaseResult.Failed("Transaction hata");
        }

        if (!success.Value)
            return PurchaseResult.Failed("Yetersiz bakiye veya ürün alınmış");

        if (purchaseSound != null)
        {
            audioService?.Play(purchaseSound);
        }

        return PurchaseResult.Success(qrPayload);
    }

    public async Task<PurchaseResult> ClaimSlotProductRewardAsync(ProductConfig productConfig)
    {
        if (!IsReady || db == null)
        {
            return PurchaseResult.Failed("Firebase hazir degil");
        }

        if (productConfig == null || string.IsNullOrEmpty(productConfig.Id))
        {
            return PurchaseResult.Failed("Gecersiz urun");
        }

        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return PurchaseResult.Failed("Gecersiz kullanici");
        }

        await EnsureUserDocumentAsync(userId);

        string qrId = Guid.NewGuid().ToString("N");
        string rewardQrPayload = BuildQrPayload(userId, productConfig.Id, qrId);

        DocumentReference userRef = db.Collection(UsersCollection).Document(userId);
        DocumentReference purchasedRef = userRef.Collection(PurchasedProductsCollection).Document(productConfig.Id);
        DocumentReference qrRef = userRef.Collection(QrCodesCollection).Document(qrId);

        return await ExecuteFirestoreOperationWithRetryAsync(
            "Slot product reward transaction",
            async () => await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot purchasedSnap = await transaction.GetSnapshotAsync(purchasedRef);
                if (purchasedSnap.Exists)
                {
                    string existingPayload = string.Empty;
                    purchasedSnap.TryGetValue("qrPayload", out existingPayload);
                    return PurchaseResult.Success(existingPayload);
                }

                var priceDetails = new List<Dictionary<string, object>>();
                const string spentSummary = "slot_reward";

                transaction.Set(
                    purchasedRef,
                    new Dictionary<string, object>
                    {
                        { "productId", productConfig.Id },
                        { "productName", productConfig.Name ?? string.Empty },
                        { "productDescription", productConfig.Description ?? string.Empty },
                        { "section", productConfig.section.ToString() },
                        { "qrId", qrId },
                        { "qrPayload", rewardQrPayload },
                        { "prices", priceDetails },
                        { "spentSummary", spentSummary },
                        { "createdAt", FieldValue.ServerTimestamp },
                        { "status", "purchased" },
                        { "source", "slot_reward" }
                    });

                transaction.Set(
                    qrRef,
                    new Dictionary<string, object>
                    {
                        { "id", qrId },
                        { "productId", productConfig.Id },
                        { "productName", productConfig.Name ?? string.Empty },
                        { "productDescription", productConfig.Description ?? string.Empty },
                        { "section", productConfig.section.ToString() },
                        { "userId", userId },
                        { "payload", rewardQrPayload },
                        { "prices", priceDetails },
                        { "spentSummary", spentSummary },
                        { "createdAt", FieldValue.ServerTimestamp },
                        { "status", "active" },
                        { "source", "slot_reward" }
                    });

                return PurchaseResult.Success(rewardQrPayload);
            }),
            e => PurchaseResult.Failed(GetExceptionSummary(e)));
    }

    public Task<Dictionary<string, List<ProductConfig.ProductPrice>>> GetShopPricesAsync()
    {
        if (UseCloudFunctionsForEconomy())
        {
            return CallShopConfigFunctionAsync();
        }

        return Task.FromResult<Dictionary<string, List<ProductConfig.ProductPrice>>>(null);
    }

    public async Task<RunStartResult> TryStartRunAsync(int playEnergyCost)
    {
        if (UseCloudFunctionsForEconomy())
        {
            return await CallStartRunFunctionAsync(playEnergyCost);
        }

        if (!IsReady || db == null)
        {
            return RunStartResult.Failed("Firebase hazir degil");
        }

        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RunStartResult.Failed("Gecersiz kullanici");
        }

        int cost = Mathf.Clamp(playEnergyCost, 0, InitialEnergyAmount);
        string runId = Guid.NewGuid().ToString("N");
        DocumentReference userRef = db.Collection(UsersCollection).Document(userId);
        DocumentReference energyRef = userRef.Collection(CurrenciesCollection).Document(EnergyCurrencyId);
        DocumentReference runRef = userRef.Collection(RunSessionsCollection).Document(runId);
        DocumentReference eventRef = userRef.Collection(CurrencyEventsCollection).Document("run_start_" + runId);

        return await ExecuteFirestoreOperationWithRetryAsync(
            "Start run transaction",
            async () => await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot energySnapshot = await transaction.GetSnapshotAsync(energyRef);
                int currentEnergy = ReadCurrencyAmount(energySnapshot);

                if (currentEnergy < cost)
                {
                    return RunStartResult.Insufficient(currentEnergy);
                }

                int nextEnergy = currentEnergy - cost;
                transaction.Set(
                    energyRef,
                    new Dictionary<string, object>
                    {
                        { "amount", nextEnergy },
                        { "updatedAt", FieldValue.ServerTimestamp }
                    },
                    SetOptions.MergeAll);

                transaction.Set(
                    runRef,
                    new Dictionary<string, object>
                    {
                        { "runId", runId },
                        { "source", "run" },
                        { "status", "started" },
                        { "startedAt", FieldValue.ServerTimestamp },
                        { "energyCost", cost },
                        { "maxCoinPerSecond", MaxCoinPerSecond },
                        { "createdAt", FieldValue.ServerTimestamp }
                    });

                transaction.Set(
                    eventRef,
                    new Dictionary<string, object>
                    {
                        { "eventId", eventRef.Id },
                        { "source", "run_start" },
                        { "runId", runId },
                        { "currencyId", EnergyCurrencyId },
                        { "amount", -cost },
                        { "balanceAfter", nextEnergy },
                        { "createdAt", FieldValue.ServerTimestamp }
                    });

                return RunStartResult.Success(runId, nextEnergy);
            }),
            e => RunStartResult.Failed(GetExceptionSummary(e)));
    }

    public async Task<RewardClaimResult> ClaimRunRewardsAsync(
        string runId,
        Dictionary<string, int> rewards)
    {
        if (UseCloudFunctionsForEconomy())
        {
            return await CallRunRewardFunctionAsync(runId, rewards);
        }

        if (!IsReady || db == null)
        {
            return RewardClaimResult.Failed("Firebase hazir degil");
        }

        if (string.IsNullOrEmpty(runId))
        {
            return RewardClaimResult.Failed("Run oturumu bulunamadi");
        }

        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RewardClaimResult.Failed("Gecersiz kullanici");
        }

        Dictionary<string, int> requestedRewards = SanitizeRewards(rewards);
        DocumentReference userRef = db.Collection(UsersCollection).Document(userId);
        DocumentReference runRef = userRef.Collection(RunSessionsCollection).Document(runId);

        return await ExecuteFirestoreOperationWithRetryAsync(
            "Claim run rewards transaction",
            async () => await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot runSnapshot = await transaction.GetSnapshotAsync(runRef);
                if (runSnapshot == null || !runSnapshot.Exists)
                {
                    return RewardClaimResult.Failed("Run oturumu bulunamadi");
                }

                string status = string.Empty;
                runSnapshot.TryGetValue("status", out status);
                if (status == "claimed")
                {
                    return RewardClaimResult.Failed("Run odulu daha once alinmis");
                }

                if (!TryReadTimestamp(runSnapshot, "startedAt", out DateTime startedAtUtc))
                {
                    return RewardClaimResult.Failed("Run baslangic zamani gecersiz");
                }

                float durationSeconds = Mathf.Max(
                    0f,
                    (float)(DateTime.UtcNow - startedAtUtc).TotalSeconds);

                var grantedRewards = new Dictionary<string, int>();
                var balanceByCurrency = new Dictionary<string, int>();

                foreach (KeyValuePair<string, int> reward in requestedRewards)
                {
                    int grantAmount = GetRunGrantAmount(reward.Key, reward.Value, durationSeconds);
                    grantedRewards[reward.Key] = grantAmount;

                    if (grantAmount <= 0)
                    {
                        continue;
                    }

                    DocumentReference currencyRef = userRef.Collection(CurrenciesCollection).Document(reward.Key);
                    DocumentSnapshot currencySnapshot = await transaction.GetSnapshotAsync(currencyRef);
                    balanceByCurrency[reward.Key] = ReadCurrencyAmount(currencySnapshot);
                }

                foreach (KeyValuePair<string, int> reward in requestedRewards)
                {
                    grantedRewards.TryGetValue(reward.Key, out int grantAmount);
                    balanceByCurrency.TryGetValue(reward.Key, out int currentBalance);
                    int nextBalance = Mathf.Clamp(currentBalance + grantAmount, 0, int.MaxValue);

                    if (grantAmount > 0)
                    {
                        transaction.Set(
                            userRef.Collection(CurrenciesCollection).Document(reward.Key),
                            new Dictionary<string, object>
                            {
                                { "amount", nextBalance },
                                { "updatedAt", FieldValue.ServerTimestamp }
                            },
                            SetOptions.MergeAll);
                    }

                    transaction.Set(
                        userRef.Collection(CurrencyEventsCollection).Document("run_" + runId + "_" + reward.Key),
                        new Dictionary<string, object>
                        {
                            { "eventId", "run_" + runId + "_" + reward.Key },
                            { "source", "run" },
                            { "sourceDetail", "gameplay_run" },
                            { "runId", runId },
                            { "currencyId", reward.Key },
                            { "requestedAmount", reward.Value },
                            { "amount", grantAmount },
                            { "balanceAfter", nextBalance },
                            { "durationSeconds", durationSeconds },
                            { "createdAt", FieldValue.ServerTimestamp }
                        });
                }

                transaction.Set(
                    runRef,
                    new Dictionary<string, object>
                    {
                        { "status", "claimed" },
                        { "claimedAt", FieldValue.ServerTimestamp },
                        { "durationSeconds", durationSeconds },
                        { "requestedRewards", requestedRewards },
                        { "grantedRewards", grantedRewards }
                    },
                    SetOptions.MergeAll);

                return RewardClaimResult.Success(grantedRewards, durationSeconds);
            }),
            e => RewardClaimResult.Failed(GetExceptionSummary(e)));
    }

    public async Task<SpinRewardClaimResult> ClaimSpinRewardAsync(int segmentCount)
    {
        if (UseCloudFunctionsForEconomy())
        {
            return await CallSpinRewardFunctionAsync(segmentCount);
        }

        if (!IsReady || db == null)
        {
            return SpinRewardClaimResult.Failed("Firebase hazir degil");
        }

        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return SpinRewardClaimResult.Failed("Gecersiz kullanici");
        }

        SpinRewardDefinition reward = GetDefaultSpinReward(segmentCount);
        DocumentReference userRef = db.Collection(UsersCollection).Document(userId);
        DocumentReference energyRef = userRef.Collection(CurrenciesCollection).Document(EnergyCurrencyId);
        DocumentReference currencyRef = userRef.Collection(CurrenciesCollection).Document(reward.CurrencyId);
        DocumentReference eventRef = userRef.Collection(CurrencyEventsCollection).Document();

        return await ExecuteFirestoreOperationWithRetryAsync(
            "Claim spin reward transaction",
            async () => await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot energySnapshot = await transaction.GetSnapshotAsync(energyRef);
                int currentEnergy = ReadCurrencyAmount(energySnapshot);

                if (currentEnergy < DefaultSpinEnergyCost)
                {
                    return SpinRewardClaimResult.Insufficient(currentEnergy);
                }

                DocumentSnapshot currencySnapshot = await transaction.GetSnapshotAsync(currencyRef);
                int currentBalance = ReadCurrencyAmount(currencySnapshot);
                int nextBalance = Mathf.Clamp(currentBalance + reward.Amount, 0, int.MaxValue);
                int nextEnergy = currentEnergy - DefaultSpinEnergyCost;

                transaction.Set(
                    energyRef,
                    new Dictionary<string, object>
                    {
                        { "amount", nextEnergy },
                        { "updatedAt", FieldValue.ServerTimestamp }
                    },
                    SetOptions.MergeAll);

                transaction.Set(
                    currencyRef,
                    new Dictionary<string, object>
                    {
                        { "amount", nextBalance },
                        { "updatedAt", FieldValue.ServerTimestamp }
                    },
                    SetOptions.MergeAll);

                transaction.Set(
                    eventRef,
                    new Dictionary<string, object>
                    {
                        { "eventId", eventRef.Id },
                        { "source", "spin" },
                        { "sourceDetail", "spin_reward" },
                        { "rewardId", reward.RewardId },
                        { "segmentIndex", reward.SegmentIndex },
                        { "currencyId", reward.CurrencyId },
                        { "requestedAmount", reward.Amount },
                        { "amount", reward.Amount },
                        { "balanceAfter", nextBalance },
                        { "energyCost", DefaultSpinEnergyCost },
                        { "energyBalanceAfter", nextEnergy },
                        { "createdAt", FieldValue.ServerTimestamp }
                    });

                return SpinRewardClaimResult.Success(
                    reward.CurrencyId,
                    reward.Amount,
                    nextBalance,
                    nextEnergy,
                    reward.SegmentIndex,
                    reward.RewardId);
            }),
            e => SpinRewardClaimResult.Failed(GetExceptionSummary(e)));
    }

    // --------------------------------------------------
    // SYNC CURRENCY
    // --------------------------------------------------
    public async Task SyncCurrencyAmountAsync(string currencyId, int amount)
    {
        if (!IsReady || db == null || string.IsNullOrEmpty(currencyId))
        {
            return;
        }

        if (currencyId != EnergyCurrencyId && amount > 0)
        {
            Debug.LogWarning("[Firestore] Positive currency sync is blocked on the client. currency=" + currencyId);
            return;
        }

        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        DocumentReference currencyRef =
            db.Collection(UsersCollection)
              .Document(userId)
              .Collection(CurrenciesCollection)
              .Document(currencyId);

        await ExecuteFirestoreOperationWithRetryAsync(
            "Sync currency amount",
            async () =>
            {
                await currencyRef.SetAsync(new Dictionary<string, object>
                {
                    { "amount", Mathf.Clamp(amount, 0, int.MaxValue) },
                    { "updatedAt", FieldValue.ServerTimestamp }
                }, SetOptions.MergeAll);

                return true;
            },
            _ => false);
    }

    public async Task<CurrencyMutationResult> ModifyCurrencyAmountAsync(string currencyId, int delta)
    {
        if (!IsReady || db == null)
        {
            return CurrencyMutationResult.Failed("Firebase hazir degil");
        }

        if (string.IsNullOrEmpty(currencyId))
        {
            return CurrencyMutationResult.Failed("Gecersiz currency");
        }

        if (delta > 0)
        {
            return CurrencyMutationResult.Failed("Pozitif currency server reward claim ile verilir");
        }

        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return CurrencyMutationResult.Failed("Gecersiz kullanici");
        }

        DocumentReference currencyRef =
            db.Collection(UsersCollection)
              .Document(userId)
              .Collection(CurrenciesCollection)
              .Document(currencyId);

        return await ExecuteFirestoreOperationWithRetryAsync(
            "Modify currency amount",
            async () => await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(currencyRef);
                int currentBalance = ReadCurrencyAmount(snapshot);

                if (delta < 0 && currentBalance < Math.Abs(delta))
                {
                    return CurrencyMutationResult.Insufficient(currentBalance);
                }

                int nextBalance = Mathf.Clamp(currentBalance + delta, 0, int.MaxValue);
                transaction.Set(
                    currencyRef,
                    new Dictionary<string, object>
                    {
                        { "amount", nextBalance },
                        { "updatedAt", FieldValue.ServerTimestamp }
                    },
                    SetOptions.MergeAll);

                return CurrencyMutationResult.Success(nextBalance);
            }),
            e => CurrencyMutationResult.Failed(GetExceptionSummary(e)));
    }

    public async Task<Dictionary<string, int>> GetCurrencyAmountsAsync()
    {
        if (!IsReady || db == null)
        {
            return null;
        }

        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return await ExecuteFirestoreOperationWithRetryAsync<Dictionary<string, int>>(
            "Get currency amounts",
            async () =>
            {
                DocumentReference userRef = db.Collection(UsersCollection).Document(userId);
                QuerySnapshot snapshot = await userRef.Collection(CurrenciesCollection).GetSnapshotAsync();

                var currencies = new Dictionary<string, int>();

                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (!document.Exists)
                    {
                        continue;
                    }

                    int amount = 0;

                    if (document.TryGetValue("amount", out long amountLong))
                    {
                        amount = Convert.ToInt32(Math.Max(0L, Math.Min(int.MaxValue, amountLong)));
                    }
                    else if (document.TryGetValue("amount", out int amountInt))
                    {
                        amount = amountInt;
                    }

                    currencies[document.Id] = Mathf.Clamp(amount, 0, int.MaxValue);
                }

                return currencies;
            },
            _ => null);
    }

    public async Task<CurrencyMutationResult> GetCurrencyAmountAsync(string currencyId)
    {
        if (string.IsNullOrEmpty(currencyId))
        {
            return CurrencyMutationResult.Failed("Geçersiz currency");
        }

        if (!IsReady || db == null)
        {
            return CurrencyMutationResult.Failed("Firebase hazır değil");
        }

        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return CurrencyMutationResult.Failed("Geçersiz user");
        }

        DocumentReference currencyRef = db
            .Collection(UsersCollection)
            .Document(userId)
            .Collection(CurrenciesCollection)
            .Document(currencyId);

        return await ExecuteFirestoreOperationWithRetryAsync(
            "Get currency amount",
            async () =>
            {
                DocumentSnapshot snapshot = await currencyRef.GetSnapshotAsync();
                int currentBalance = ReadCurrencyAmount(snapshot);

                return CurrencyMutationResult.Success(currentBalance);
            },
            e => CurrencyMutationResult.Failed(GetExceptionSummary(e)));
    }

    public IDisposable ListenToCurrencyAmount(string currencyId, Action<int> onChanged)
    {
        if (!IsReady || db == null || string.IsNullOrEmpty(currencyId) || onChanged == null)
        {
            return null;
        }

        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        DocumentReference currencyRef =
            db.Collection(UsersCollection)
              .Document(userId)
              .Collection(CurrenciesCollection)
              .Document(currencyId);

        return currencyRef.Listen(snapshot =>
        {
            try
            {
                int amount = 0;

                if (snapshot != null && snapshot.Exists)
                {
                    if (snapshot.TryGetValue("amount", out long amountLong))
                    {
                        amount = Convert.ToInt32(Math.Max(0L, Math.Min(int.MaxValue, amountLong)));
                    }
                    else if (snapshot.TryGetValue("amount", out int amountInt))
                    {
                        amount = amountInt;
                    }
                }

                onChanged(Mathf.Clamp(amount, 0, int.MaxValue));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Firestore] Currency listener callback skipped: " + GetExceptionSummary(e));
            }
        });
    }

    public async Task<EnergyTransactionResult> GetEnergyAmountAsync(int maxEnergy)
    {
        if (!IsReady || db == null)
        {
            return EnergyTransactionResult.Failed("Firebase hazır değil");
        }

        maxEnergy = Math.Max(1, maxEnergy);
        DocumentReference energyRef = db
            .Collection(UsersCollection)
            .Document(GetUserId())
            .Collection(CurrenciesCollection)
            .Document(EnergyCurrencyId);

        return await ExecuteFirestoreOperationWithRetryAsync(
            "Get energy amount",
            async () =>
            {
                DocumentSnapshot snapshot = await energyRef.GetSnapshotAsync();
                int currentBalance = ReadCurrencyAmount(snapshot);

                return EnergyTransactionResult.Success(Math.Min(currentBalance, maxEnergy), false);
            },
            e => EnergyTransactionResult.Failed(GetExceptionSummary(e)));
    }

    public async Task<EnergyTransactionResult> ClaimDailyEnergyAsync(
        int maxEnergy,
        int dailyEnergy,
        float cooldownHours)
    {
        if (!IsReady || db == null)
        {
            return EnergyTransactionResult.Failed("Firebase hazır değil");
        }

        maxEnergy = Math.Max(1, maxEnergy);
        DocumentReference energyRef = db
            .Collection(UsersCollection)
            .Document(GetUserId())
            .Collection(CurrenciesCollection)
            .Document(EnergyCurrencyId);

        return await ExecuteFirestoreOperationWithRetryAsync(
            "Read energy balance",
            async () =>
            {
                DocumentSnapshot snapshot = await energyRef.GetSnapshotAsync();
                int currentBalance = ReadCurrencyAmount(snapshot);

                return EnergyTransactionResult.Success(Math.Min(currentBalance, maxEnergy), false);
            },
            e => EnergyTransactionResult.Failed(GetExceptionSummary(e)));
    }

    public async Task<EnergyTransactionResult> TrySpendEnergyAsync(int amount)
    {
        if (!IsReady || db == null)
        {
            return EnergyTransactionResult.Failed("Firebase hazır değil");
        }

        if (amount <= 0)
        {
            return EnergyTransactionResult.Success(0, false);
        }

        DocumentReference energyRef = db
            .Collection(UsersCollection)
            .Document(GetUserId())
            .Collection(CurrenciesCollection)
            .Document(EnergyCurrencyId);

        return await ExecuteFirestoreOperationWithRetryAsync(
            "Spend energy transaction",
            async () => await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(energyRef);
                int currentBalance = ReadCurrencyAmount(snapshot);

                if (currentBalance < amount)
                {
                    return EnergyTransactionResult.Insufficient(currentBalance);
                }

                int nextBalance = currentBalance - amount;
                transaction.Set(
                    energyRef,
                    new Dictionary<string, object>
                    {
                        { "amount", nextBalance },
                        { "updatedAt", FieldValue.ServerTimestamp }
                    },
                    SetOptions.MergeAll);

                return EnergyTransactionResult.Success(nextBalance, false);
            }),
            e => EnergyTransactionResult.Failed(GetExceptionSummary(e)));
    }

    public async Task<FreeSpinTransactionResult> TryUseFreeSpinAsync(float cooldownHours)
    {
        if (!IsReady || db == null)
        {
            return FreeSpinTransactionResult.Failed("Firebase hazır değil");
        }

        cooldownHours = Math.Max(1f, cooldownHours);

        DocumentReference dailySpinRef = db
            .Collection(UsersCollection)
            .Document(GetUserId())
            .Collection(EntitlementsCollection)
            .Document(DailySpinDocument);

        return await ExecuteFirestoreOperationWithRetryAsync(
            "Free spin transaction",
            async () => await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(dailySpinRef);
                bool isAvailable = !TryReadTimestamp(snapshot, "lastUsedAt", out DateTime lastUsedUtc) ||
                                   DateTime.UtcNow - lastUsedUtc >= TimeSpan.FromHours(cooldownHours);

                if (!isAvailable)
                {
                    return FreeSpinTransactionResult.Cooldown();
                }

                transaction.Set(
                    dailySpinRef,
                    new Dictionary<string, object>
                    {
                        { "lastUsedAt", FieldValue.ServerTimestamp }
                    },
                    SetOptions.MergeAll);

                return FreeSpinTransactionResult.Success();
            }),
            e => FreeSpinTransactionResult.Failed(GetExceptionSummary(e)));
    }

    private static int ReadCurrencyAmount(DocumentSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.Exists)
        {
            return 0;
        }

        if (snapshot.TryGetValue("amount", out long amountLong))
        {
            return Convert.ToInt32(Math.Max(0L, Math.Min(int.MaxValue, amountLong)));
        }

        if (snapshot.TryGetValue("amount", out int amountInt))
        {
            return Mathf.Clamp(amountInt, 0, int.MaxValue);
        }

        return 0;
    }

    private static Dictionary<string, int> SanitizeRewards(Dictionary<string, int> rewards)
    {
        var result = new Dictionary<string, int>();
        if (rewards == null)
        {
            return result;
        }

        foreach (KeyValuePair<string, int> reward in rewards)
        {
            if (string.IsNullOrEmpty(reward.Key) ||
                reward.Key == EnergyCurrencyId ||
                reward.Value <= 0)
            {
                continue;
            }

            result.TryGetValue(reward.Key, out int currentAmount);
            result[reward.Key] = Mathf.Clamp(currentAmount + reward.Value, 0, int.MaxValue);
        }

        return result;
    }

    private static int GetRunGrantAmount(string currencyId, int requestedAmount, float durationSeconds)
    {
        if (string.IsNullOrEmpty(currencyId) || requestedAmount <= 0 || durationSeconds <= 0f)
        {
            return 0;
        }

        int capPerSecond = currencyId == "coin" ||
                           currencyId == "coffee" ||
                           currencyId == "matcha" ||
                           currencyId == "cookie"
            ? MaxCoinPerSecond
            : 0;
        int maxAllowed = Mathf.FloorToInt(durationSeconds * capPerSecond);
        return Mathf.Clamp(Mathf.Min(requestedAmount, maxAllowed), 0, int.MaxValue);
    }

    private static SpinRewardDefinition GetDefaultSpinReward(int segmentCount)
    {
        int safeSegmentCount = Mathf.Max(1, segmentCount);
        int selectedIndex = UnityEngine.Random.Range(0, safeSegmentCount);
        int normalizedIndex = selectedIndex % 4;

        switch (normalizedIndex)
        {
            case 1:
                return new SpinRewardDefinition(selectedIndex, "spin_coffee_2", "coffee", 2);
            case 2:
                return new SpinRewardDefinition(selectedIndex, "spin_matcha_2", "matcha", 2);
            case 3:
                return new SpinRewardDefinition(selectedIndex, "spin_cookie_2", "cookie", 2);
            default:
                return new SpinRewardDefinition(selectedIndex, "spin_coin_20", "coin", 20);
        }
    }

    private readonly struct SpinRewardDefinition
    {
        public readonly int SegmentIndex;
        public readonly string RewardId;
        public readonly string CurrencyId;
        public readonly int Amount;

        public SpinRewardDefinition(int segmentIndex, string rewardId, string currencyId, int amount)
        {
            SegmentIndex = segmentIndex;
            RewardId = rewardId;
            CurrencyId = currencyId;
            Amount = amount;
        }
    }

    private static bool TryReadTimestamp(
        DocumentSnapshot snapshot,
        string fieldName,
        out DateTime utcDateTime)
    {
        utcDateTime = default;

        if (snapshot == null ||
            !snapshot.Exists ||
            !snapshot.TryGetValue(fieldName, out Timestamp timestamp))
        {
            return false;
        }

        utcDateTime = timestamp.ToDateTime().ToUniversalTime();
        return true;
    }

    public async Task<Dictionary<string, string>> GetActivePurchasedProductsAsync()
    {
        if (!IsReady || db == null)
        {
            return null;
        }

        string userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return await ExecuteFirestoreOperationWithRetryAsync<Dictionary<string, string>>(
            "Get active purchased products",
            async () =>
            {
                DocumentReference userRef = db.Collection(UsersCollection).Document(userId);

                QuerySnapshot purchasedSnapshot =
                    await userRef.Collection(PurchasedProductsCollection).GetSnapshotAsync();

                var activeProducts = new Dictionary<string, string>();

                foreach (DocumentSnapshot purchasedDocument in purchasedSnapshot.Documents)
                {
                    if (!purchasedDocument.Exists)
                    {
                        continue;
                    }

                    string productId = purchasedDocument.Id;
                    if (purchasedDocument.TryGetValue("productId", out string productIdField) && !string.IsNullOrEmpty(productIdField))
                    {
                        productId = productIdField;
                    }

                    string qrId = string.Empty;
                    purchasedDocument.TryGetValue("qrId", out qrId);

                    string qrPayload = string.Empty;
                    purchasedDocument.TryGetValue("qrPayload", out qrPayload);

                    if (string.IsNullOrEmpty(qrId))
                    {
                        continue;
                    }

                    DocumentSnapshot qrSnapshot =
                        await userRef.Collection(QrCodesCollection).Document(qrId).GetSnapshotAsync();

                    if (!qrSnapshot.Exists)
                    {
                        continue;
                    }

                    string status = string.Empty;
                    qrSnapshot.TryGetValue("status", out status);

                    if (!string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(qrPayload) && qrSnapshot.TryGetValue("payload", out string payloadFromQr))
                    {
                        qrPayload = payloadFromQr;
                    }

                    activeProducts[productId] = qrPayload;
                }

                return activeProducts;
            },
            _ => null);
    }

    // --------------------------------------------------
    // QR PAYLOAD
    // --------------------------------------------------
    private static List<Dictionary<string, object>> BuildPriceDetails(
        Dictionary<string, int> totalCostByCurrency)
    {
        var details = new List<Dictionary<string, object>>();
        if (totalCostByCurrency == null)
        {
            return details;
        }

        foreach (KeyValuePair<string, int> currencyCost in totalCostByCurrency)
        {
            details.Add(new Dictionary<string, object>
            {
                { "currency", currencyCost.Key },
                { "amount", Mathf.Max(0, currencyCost.Value) }
            });
        }

        return details;
    }

    private static string FormatPriceSummary(Dictionary<string, int> totalCostByCurrency)
    {
        if (totalCostByCurrency == null || totalCostByCurrency.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (KeyValuePair<string, int> currencyCost in totalCostByCurrency)
        {
            parts.Add(Mathf.Max(0, currencyCost.Value) + " " + currencyCost.Key);
        }

        return string.Join(", ", parts);
    }

    private static string BuildQrPayload(string userId, string productId, string qrId)
    {
        return $"rocqr:v1:{userId}:{productId}:{qrId}";
    }
}
#endif

// --------------------------------------------------
// RESULT STRUCT
// --------------------------------------------------
public struct PurchaseResult
{
    public bool IsSuccess;
    public string QrPayload;
    public string Error;

    public static PurchaseResult Success(string payload)
    {
        return new PurchaseResult
        {
            IsSuccess = true,
            QrPayload = payload,
            Error = ""
        };
    }

    public static PurchaseResult Failed(string error)
    {
        return new PurchaseResult
        {
            IsSuccess = false,
            Error = error,
            QrPayload = ""
        };
    }
}

public struct EnergyTransactionResult
{
    public bool IsSuccess;
    public bool IsInsufficient;
    public bool WasRefilled;
    public int Balance;
    public string Error;

    public static EnergyTransactionResult Success(int balance, bool wasRefilled)
    {
        return new EnergyTransactionResult
        {
            IsSuccess = true,
            Balance = Math.Max(0, balance),
            WasRefilled = wasRefilled,
            Error = string.Empty
        };
    }

    public static EnergyTransactionResult Insufficient(int balance)
    {
        return new EnergyTransactionResult
        {
            IsSuccess = false,
            IsInsufficient = true,
            Balance = Math.Max(0, balance),
            Error = "Yetersiz enerji"
        };
    }

    public static EnergyTransactionResult Failed(string error)
    {
        return new EnergyTransactionResult
        {
            IsSuccess = false,
            Error = error ?? string.Empty
        };
    }
}

public struct CurrencyMutationResult
{
    public bool IsSuccess;
    public bool IsInsufficient;
    public int Balance;
    public string Error;

    public static CurrencyMutationResult Success(int balance)
    {
        return new CurrencyMutationResult
        {
            IsSuccess = true,
            Balance = Math.Max(0, balance),
            Error = string.Empty
        };
    }

    public static CurrencyMutationResult Insufficient(int balance)
    {
        return new CurrencyMutationResult
        {
            IsSuccess = false,
            IsInsufficient = true,
            Balance = Math.Max(0, balance),
            Error = "Yetersiz bakiye"
        };
    }

    public static CurrencyMutationResult Failed(string error)
    {
        return new CurrencyMutationResult
        {
            IsSuccess = false,
            Error = string.IsNullOrEmpty(error) ? "Currency transaction hata" : error
        };
    }
}

public struct RunStartResult
{
    public bool IsSuccess;
    public bool IsInsufficient;
    public string RunId;
    public int EnergyBalance;
    public string Error;

    public static RunStartResult Success(string runId, int energyBalance)
    {
        return new RunStartResult
        {
            IsSuccess = true,
            RunId = runId ?? string.Empty,
            EnergyBalance = Math.Max(0, energyBalance),
            Error = string.Empty
        };
    }

    public static RunStartResult Insufficient(int energyBalance)
    {
        return new RunStartResult
        {
            IsSuccess = false,
            IsInsufficient = true,
            EnergyBalance = Math.Max(0, energyBalance),
            Error = "Yetersiz enerji"
        };
    }

    public static RunStartResult Failed(string error)
    {
        return new RunStartResult
        {
            IsSuccess = false,
            Error = string.IsNullOrEmpty(error) ? "Run transaction hata" : error
        };
    }
}

public struct RewardClaimResult
{
    public bool IsSuccess;
    public Dictionary<string, int> Grants;
    public float DurationSeconds;
    public string Error;

    public static RewardClaimResult Success(Dictionary<string, int> grants, float durationSeconds)
    {
        return new RewardClaimResult
        {
            IsSuccess = true,
            Grants = grants ?? new Dictionary<string, int>(),
            DurationSeconds = durationSeconds < 0f ? 0f : durationSeconds,
            Error = string.Empty
        };
    }

    public static RewardClaimResult Failed(string error)
    {
        return new RewardClaimResult
        {
            IsSuccess = false,
            Grants = new Dictionary<string, int>(),
            Error = string.IsNullOrEmpty(error) ? "Reward claim hata" : error
        };
    }
}

public struct SpinRewardClaimResult
{
    public bool IsSuccess;
    public bool IsInsufficient;
    public string CurrencyId;
    public int Amount;
    public int Balance;
    public int EnergyBalance;
    public int SegmentIndex;
    public string RewardId;
    public string Error;

    public static SpinRewardClaimResult Success(
        string currencyId,
        int amount,
        int balance,
        int energyBalance,
        int segmentIndex,
        string rewardId)
    {
        return new SpinRewardClaimResult
        {
            IsSuccess = true,
            CurrencyId = currencyId ?? string.Empty,
            Amount = Math.Max(0, amount),
            Balance = Math.Max(0, balance),
            EnergyBalance = Math.Max(0, energyBalance),
            SegmentIndex = Math.Max(0, segmentIndex),
            RewardId = rewardId ?? string.Empty,
            Error = string.Empty
        };
    }

    public static SpinRewardClaimResult Insufficient(int energyBalance)
    {
        return new SpinRewardClaimResult
        {
            IsSuccess = false,
            IsInsufficient = true,
            EnergyBalance = Math.Max(0, energyBalance),
            Error = "Yetersiz enerji"
        };
    }

    public static SpinRewardClaimResult Failed(string error)
    {
        return new SpinRewardClaimResult
        {
            IsSuccess = false,
            Error = string.IsNullOrEmpty(error) ? "Spin reward claim hata" : error
        };
    }
}

public struct FreeSpinTransactionResult
{
    public bool IsSuccess;
    public bool IsOnCooldown;
    public string Error;

    public static FreeSpinTransactionResult Success()
    {
        return new FreeSpinTransactionResult
        {
            IsSuccess = true,
            Error = string.Empty
        };
    }

    public static FreeSpinTransactionResult Cooldown()
    {
        return new FreeSpinTransactionResult
        {
            IsSuccess = false,
            IsOnCooldown = true,
            Error = string.Empty
        };
    }

    public static FreeSpinTransactionResult Failed(string error)
    {
        return new FreeSpinTransactionResult
        {
            IsSuccess = false,
            Error = error ?? string.Empty
        };
    }
}
