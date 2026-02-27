using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Game.Systems;
using UnityEngine;

public class FirestoreGameSecurityService : MonoBehaviour
{
    private const string UsersCollection = "users";
    private const string CurrenciesCollection = "currencies";
    private const string QrCodesCollection = "QRCodes";
    private const string PurchasedProductsCollection = "purchasedProducts";
    private const string LocalUserIdPrefsKey = "FirestoreGameSecurityService.UserId";

    public static FirestoreGameSecurityService Instance { get; private set; }

    public bool IsReady { get; private set; }

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string activeUserId;
    private const int EnsureUserDocMaxRetryCount = 3;

    // --------------------------------------------------
    // AUTO BOOTSTRAP
    // --------------------------------------------------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        GameObject serviceObject = new GameObject("FirestoreGameSecurityService");
        serviceObject.AddComponent<FirestoreGameSecurityService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

    }

    // --------------------------------------------------
    // FIREBASE INIT + AUTH
    // --------------------------------------------------
    private async void Start()
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            DependencyStatus dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus != DependencyStatus.Available)
            {
                Debug.LogError("Firebase init failed: " + dependencyStatus);
                return;
            }

            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;

            activeUserId = await ResolveActiveUserIdAsync();
            await WarmupAuthSessionAsync();

            IsReady = true;

            await EnsureUserDocumentAsync(GetUserId());
            Debug.Log("✅ User document ensured");
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Firebase initialize failed: " + e);
        }
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
            try
            {
                Debug.Log("ℹ️ Firebase current user not found. Trying anonymous sign-in...");
                var authResult = await auth.SignInAnonymouslyAsync();
                currentUser = authResult.User;
                Debug.Log("✅ Firebase anonymous user created: " + currentUser.UserId);
            }
            catch (Exception e)
            {
                string message =
                    "❌ Firebase anonymous sign-in failed. " +
                    "Firebase Console > Authentication > Sign-in method ekranında Anonymous provider'ı etkinleştir ve tekrar dene. " +
                    "Detay: " + e;

                Debug.LogError(message);
                throw;
            }
        }

        if (currentUser == null || string.IsNullOrEmpty(currentUser.UserId))
        {
            throw new InvalidOperationException("Firebase anonymous sign-in did not return a valid user.");
        }
        else
        {
            Debug.Log("✅ Firebase existing user restored: " + currentUser.UserId);
        }

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

    private async Task EnsureFreshAuthTokenAsync()
    {
        if (auth?.CurrentUser == null)
        {
            throw new InvalidOperationException("Auth token refresh failed because CurrentUser is null.");
        }

        string token = await auth.CurrentUser.TokenAsync(true);
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Auth token refresh returned an empty token.");
        }

        Debug.Log($"✅ Firebase auth token refreshed for uid: {auth.CurrentUser.UserId}");
    }

    private async Task WarmupAuthSessionAsync()
    {
        try
        {
            await EnsureFreshAuthTokenAsync();
            return;
        }
        catch (Exception firstException)
        {
            Debug.LogWarning($"⚠️ Forced token refresh failed. Trying soft refresh... {firstException.Message}");
        }

        if (auth?.CurrentUser == null)
        {
            throw new InvalidOperationException("Auth warmup failed because CurrentUser is null.");
        }

        try
        {
            string token = await auth.CurrentUser.TokenAsync(false);
            if (!string.IsNullOrEmpty(token))
            {
                Debug.Log($"✅ Firebase auth soft token refresh succeeded for uid: {auth.CurrentUser.UserId}");
                return;
            }
        }
        catch (Exception secondException)
        {
            Debug.LogWarning($"⚠️ Soft token refresh failed. Recreating anonymous session... {secondException.Message}");
        }

        auth.SignOut();
        var authResult = await auth.SignInAnonymouslyAsync();
        if (authResult?.User == null || string.IsNullOrEmpty(authResult.User.UserId))
        {
            throw new InvalidOperationException("Auth warmup failed: Could not create fallback anonymous user.");
        }

        activeUserId = authResult.User.UserId;
        SaveLocalUserId(activeUserId);
        await EnsureFreshAuthTokenAsync();
        Debug.Log($"✅ Firebase auth session recreated for uid: {activeUserId}");
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

        DocumentReference userRef = db.Collection(UsersCollection).Document(userId);

        await DeleteCollectionDocumentsAsync(userRef.Collection(CurrenciesCollection));
        await DeleteCollectionDocumentsAsync(userRef.Collection(QrCodesCollection));
        await DeleteCollectionDocumentsAsync(userRef.Collection(PurchasedProductsCollection));

        await userRef.DeleteAsync();

        PlayerPrefs.DeleteKey(LocalUserIdPrefsKey);
        PlayerPrefs.Save();

        auth.SignOut();

        activeUserId = await ResolveActiveUserIdAsync();
        await EnsureUserDocumentAsync(activeUserId);

        Debug.Log("✅ Current user data cleared and a new anonymous user was created: " + activeUserId);
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
            Debug.LogError("❌ EnsureUserDocumentAsync skipped: Auth current user is null.");
            return;
        }

        if (auth.CurrentUser.UserId != userId)
        {
            Debug.LogWarning($"⚠️ EnsureUserDocumentAsync userId mismatch. auth: {auth.CurrentUser.UserId}, requested: {userId}");
        }

        DocumentReference userRef =
            db.Collection(UsersCollection).Document(userId);

        Dictionary<string, object> userPayload = new Dictionary<string, object>
        {
            { "userId", userId },
            { "createdAt", FieldValue.ServerTimestamp },
            { "lastSeenAt", FieldValue.ServerTimestamp }
        };

        Exception lastException = null;

        for (int attempt = 1; attempt <= EnsureUserDocMaxRetryCount; attempt++)
        {
            try
            {
                await userRef.SetAsync(userPayload, SetOptions.MergeAll);
                Debug.Log($"✅ EnsureUserDocumentAsync success (attempt {attempt}) for uid: {userId}");
                return;
            }
            catch (Exception e)
            {
                lastException = e;
                string currentAuthUserId = auth?.CurrentUser?.UserId ?? "<null>";
                Debug.LogWarning(
                    $"⚠️ EnsureUserDocumentAsync attempt {attempt}/{EnsureUserDocMaxRetryCount} failed. " +
                    $"requestedUid={userId}, authUid={currentAuthUserId}, error={e.Message}");

                if (attempt < EnsureUserDocMaxRetryCount)
                {
                    await EnsureFreshAuthTokenAsync();
                    await Task.Delay(300 * attempt);
                }
            }
        }

        throw new InvalidOperationException(
            $"EnsureUserDocumentAsync failed after {EnsureUserDocMaxRetryCount} attempts for uid {userId}.",
            lastException);
    }

    // --------------------------------------------------
    // PURCHASE TRANSACTION
    // --------------------------------------------------
    public async Task<PurchaseResult> TryPurchaseProductAsync(ProductConfig productConfig)
    {
        if (!IsReady)
            return PurchaseResult.Failed("Firebase hazır değil");

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

        try
        {
            bool success = await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot purchasedSnap =
                    await transaction.GetSnapshotAsync(purchasedRef);

                if (purchasedSnap.Exists)
                    return false;

                var totalCostByCurrency = new Dictionary<string, int>();

                foreach (var price in productConfig.Prices)
                {
                    if (totalCostByCurrency.TryGetValue(price.currency, out int existingAmount))
                    {
                        totalCostByCurrency[price.currency] = existingAmount + price.amount;
                    }
                    else
                    {
                        totalCostByCurrency[price.currency] = price.amount;
                    }
                }

                var balanceByCurrency = new Dictionary<string, int>();

                // CHECK BALANCE
                foreach (var currencyCost in totalCostByCurrency)
                {
                    var currencyRef =
                        userRef.Collection(CurrenciesCollection)
                               .Document(currencyCost.Key);

                    var currencySnap =
                        await transaction.GetSnapshotAsync(currencyRef);

                    int currentBalance = 0;

                    if (currencySnap.Exists &&
                        currencySnap.TryGetValue("amount", out long amount))
                    {
                        currentBalance = Convert.ToInt32(amount);
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
                        { "qrId", qrId },
                        { "qrPayload", qrPayload },
                        { "createdAt", FieldValue.ServerTimestamp },
                        { "status", "purchased" }
                    });

                // QR DOC
                transaction.Set(qrRef,
                    new Dictionary<string, object>
                    {
                        { "id", qrId },
                        { "productId", productConfig.Id },
                        { "userId", userId },
                        { "payload", qrPayload },
                        { "createdAt", FieldValue.ServerTimestamp },
                        { "status", "active" },
                        { "source", "store_purchase" }
                    });

                return true;
            });

            if (!success)
                return PurchaseResult.Failed("Yetersiz bakiye veya ürün alınmış");

            return PurchaseResult.Success(qrPayload);
        }
        catch (Exception e)
        {
            Debug.LogError("Transaction error: " + e);
            return PurchaseResult.Failed("Transaction hata");
        }
    }

    // --------------------------------------------------
    // SYNC CURRENCY
    // --------------------------------------------------
    public async Task SyncCurrencyAmountAsync(string currencyId, int amount)
    {
        if (!IsReady) return;

        string userId = GetUserId();

        DocumentReference currencyRef =
            db.Collection(UsersCollection)
              .Document(userId)
              .Collection(CurrenciesCollection)
              .Document(currencyId);

        await currencyRef.SetAsync(new Dictionary<string, object>
        {
            { "amount", amount },
            { "updatedAt", FieldValue.ServerTimestamp }
        }, SetOptions.MergeAll);
    }

    // --------------------------------------------------
    // QR PAYLOAD
    // --------------------------------------------------
    private static string BuildQrPayload(string userId, string productId, string qrId)
    {
        return $"rocqr:v1:{userId}:{productId}:{qrId}";
    }
}

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
