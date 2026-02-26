using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using Game.Systems;
using UnityEngine;

public class FirestoreGameSecurityService : MonoBehaviour
{
    [SerializeField] private string debugUserId = "testUser";

    private const string UsersCollection = "users";
    private const string CurrenciesCollection = "currencies";
    private const string QrCodesCollection = "QRCodes";
    private const string PurchasedProductsCollection = "purchasedProducts";

    public static FirestoreGameSecurityService Instance { get; private set; }

    public bool IsReady { get; private set; }

    private FirebaseFirestore db;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

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

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogError("Firebase init failed: " + task.Result);
                return;
            }

            db = FirebaseFirestore.DefaultInstance;
            IsReady = true;

            _ = EnsureUserDocumentAsync(GetUserId());
        });
    }

    public string GetUserId()
    {
        return string.IsNullOrWhiteSpace(debugUserId) ? "testUser" : debugUserId;
    }

    public async Task EnsureUserDocumentAsync(string userId)
    {
        if (!IsReady)
        {
            return;
        }

        DocumentReference userRef = db.Collection(UsersCollection).Document(userId);
        DocumentSnapshot userSnap = await userRef.GetSnapshotAsync();

        if (!userSnap.Exists)
        {
            Dictionary<string, object> userData = new Dictionary<string, object>
            {
                { "createdAt", FieldValue.ServerTimestamp },
                { "lastSeenAt", FieldValue.ServerTimestamp }
            };

            await userRef.SetAsync(userData, SetOptions.MergeAll);
        }
        else
        {
            await userRef.UpdateAsync(new Dictionary<string, object>
            {
                { "lastSeenAt", FieldValue.ServerTimestamp }
            });
        }
    }

    public async Task<PurchaseResult> TryPurchaseProductAsync(ProductConfig productConfig)
    {
        if (!IsReady)
        {
            return PurchaseResult.Failed("Firebase hazır değil");
        }

        string userId = GetUserId();
        await EnsureUserDocumentAsync(userId);

        string qrId = Guid.NewGuid().ToString("N");
        string qrPayload = BuildQrPayload(userId, productConfig.Id, qrId);

        DocumentReference userRef = db.Collection(UsersCollection).Document(userId);
        DocumentReference purchasedRef = userRef.Collection(PurchasedProductsCollection).Document(productConfig.Id);
        DocumentReference qrRef = userRef.Collection(QrCodesCollection).Document(qrId);

        try
        {
            bool transactionResult = await db.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot purchasedSnap = await transaction.GetSnapshotAsync(purchasedRef);
                if (purchasedSnap.Exists)
                {
                    return false;
                }

                foreach (var price in productConfig.Prices)
                {
                    DocumentReference currencyRef = userRef.Collection(CurrenciesCollection).Document(price.currency);
                    DocumentSnapshot currencySnap = await transaction.GetSnapshotAsync(currencyRef);

                    int currentBalance = 0;
                    if (currencySnap.Exists && currencySnap.TryGetValue("amount", out long amountAsLong))
                    {
                        currentBalance = Convert.ToInt32(amountAsLong);
                    }

                    if (currentBalance < price.amount)
                    {
                        return false;
                    }
                }

                foreach (var price in productConfig.Prices)
                {
                    DocumentReference currencyRef = userRef.Collection(CurrenciesCollection).Document(price.currency);
                    DocumentSnapshot currencySnap = await transaction.GetSnapshotAsync(currencyRef);

                    int currentBalance = 0;
                    if (currencySnap.Exists && currencySnap.TryGetValue("amount", out long amountAsLong))
                    {
                        currentBalance = Convert.ToInt32(amountAsLong);
                    }

                    int newBalance = Math.Max(0, currentBalance - price.amount);
                    transaction.Set(currencyRef, new Dictionary<string, object>
                    {
                        { "amount", newBalance },
                        { "updatedAt", FieldValue.ServerTimestamp }
                    }, SetOptions.MergeAll);
                }

                Dictionary<string, object> purchaseData = new Dictionary<string, object>
                {
                    { "productId", productConfig.Id },
                    { "qrId", qrId },
                    { "qrPayload", qrPayload },
                    { "createdAt", FieldValue.ServerTimestamp },
                    { "status", "purchased" }
                };

                transaction.Set(purchasedRef, purchaseData, SetOptions.MergeAll);

                Dictionary<string, object> qrData = new Dictionary<string, object>
                {
                    { "id", qrId },
                    { "productId", productConfig.Id },
                    { "userId", userId },
                    { "payload", qrPayload },
                    { "createdAt", FieldValue.ServerTimestamp },
                    { "status", "active" },
                    { "source", "store_purchase" }
                };

                transaction.Set(qrRef, qrData, SetOptions.MergeAll);

                return true;
            });

            if (!transactionResult)
            {
                return PurchaseResult.Failed("Satın alma doğrulanamadı (yetersiz bakiye / ürün zaten alınmış)");
            }

            return PurchaseResult.Success(qrPayload);
        }
        catch (Exception ex)
        {
            Debug.LogError("TryPurchaseProductAsync error: " + ex);
            return PurchaseResult.Failed("Satın alma işlemi sırasında hata oluştu");
        }
    }

    public async Task SyncCurrencyAmountAsync(string currencyId, int amount)
    {
        if (!IsReady)
        {
            return;
        }

        string userId = GetUserId();
        await EnsureUserDocumentAsync(userId);

        DocumentReference currencyRef = db
            .Collection(UsersCollection)
            .Document(userId)
            .Collection(CurrenciesCollection)
            .Document(currencyId);

        await currencyRef.SetAsync(new Dictionary<string, object>
        {
            { "amount", amount },
            { "updatedAt", FieldValue.ServerTimestamp }
        }, SetOptions.MergeAll);
    }

    private static string BuildQrPayload(string userId, string productId, string qrId)
    {
        return $"rocqr:v1:{userId}:{productId}:{qrId}";
    }
}

public struct PurchaseResult
{
    public bool IsSuccess;
    public string QrPayload;
    public string Error;

    public static PurchaseResult Success(string qrPayload)
    {
        return new PurchaseResult
        {
            IsSuccess = true,
            QrPayload = qrPayload,
            Error = string.Empty
        };
    }

    public static PurchaseResult Failed(string error)
    {
        return new PurchaseResult
        {
            IsSuccess = false,
            QrPayload = string.Empty,
            Error = error
        };
    }
}
