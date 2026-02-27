using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
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
    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(async task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogError("Firebase init failed: " + task.Result);
                return;
            }

            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;

            try
            {
                activeUserId = await ResolveActiveUserIdAsync();
            }
            catch (Exception e)
            {
                Debug.LogError("❌ Auth failed: " + e);
                return;
            }

            IsReady = true;

            try
            {
                await EnsureUserDocumentAsync(GetUserId());
                Debug.Log("✅ User document ensured");
            }
            catch (Exception e)
            {
                Debug.LogError("EnsureUserDocumentAsync error: " + e);
            }
        });
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
            var authResult = await auth.SignInAnonymouslyAsync();
            currentUser = authResult.User;
            Debug.Log("✅ Firebase anonymous user created: " + currentUser.UserId);
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
            $"⚠️ Saved user id ({savedUserId}) and Firebase auth user ({currentUserId}) mismatch. Creating a brand new anonymous user to avoid selecting another existing user.");

        auth.SignOut();
        var freshAuthResult = await auth.SignInAnonymouslyAsync();
        string newUserId = freshAuthResult.User.UserId;

        SaveLocalUserId(newUserId);
        Debug.Log("✅ Firebase fresh anonymous user created: " + newUserId);

        return newUserId;
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
        if (!IsReady || string.IsNullOrEmpty(userId))
            return;

        DocumentReference userRef =
            db.Collection(UsersCollection).Document(userId);

        DocumentSnapshot snap = await userRef.GetSnapshotAsync();

        if (!snap.Exists)
        {
            await userRef.SetAsync(new Dictionary<string, object>
            {
                { "createdAt", FieldValue.ServerTimestamp },
                { "lastSeenAt", FieldValue.ServerTimestamp }
            });
        }
        else
        {
            await userRef.UpdateAsync(new Dictionary<string, object>
            {
                { "lastSeenAt", FieldValue.ServerTimestamp }
            });
        }
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
