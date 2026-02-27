using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Utils.Save;

namespace Game.Systems
{
    public class PurchasedProductsEntity : ISaveable
    {
        public List<string> purchasedProductIds;
        public Dictionary<string, string> purchasedProductQRCodesById;

        public PurchasedProductsEntity()
        {
            purchasedProductIds = new List<string>();
            purchasedProductQRCodesById = new Dictionary<string, string>();
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public T Deserialize<T>(string data) where T : ISaveable, new()
        {
            if (string.IsNullOrEmpty(data))
            {
                return JsonConvert.DeserializeObject<T>(null);
            }

            return JsonConvert.DeserializeObject<T>(data);
        }
    }

    public class StoreManager : BaseSystem
    {
        private StoreSettings storeSettings;
        public StoreSettings StoreSettings => storeSettings;

        private List<string> purchasedProductIds = new List<string>();
        public List<string> PurchasedProductIds => purchasedProductIds;
        private Dictionary<string, string> purchasedProductQRCodesById = new Dictionary<string, string>();
        private SaveRepository<PurchasedProductsEntity> purchasedProductsRepo;
        public static StoreManager Instance { get; private set; }

        public StoreManager(StoreSettings storeSettings) 
        {
            if (Instance != null && Instance != this)
            {
                Instance.Dispose();
            }

            Instance = this;

            this.storeSettings = storeSettings;

            SaveService.Instance.Register<PurchasedProductsEntity>("store_purchased_products");
            purchasedProductsRepo = SaveService.Instance.GetRepository<PurchasedProductsEntity>();
            purchasedProductsRepo.Load();

            var purchasedProducts = purchasedProductsRepo.Get();
            purchasedProductIds = purchasedProducts.purchasedProductIds ?? new List<string>();
            purchasedProductQRCodesById = purchasedProducts.purchasedProductQRCodesById ?? new Dictionary<string, string>();

            _ = ReconcilePurchasedProductsWithServerAsync();
        }

        private async Task ReconcilePurchasedProductsWithServerAsync()
        {
            FirestoreGameSecurityService firebaseService = await WaitForFirebaseServiceAsync();
            if (firebaseService == null)
            {
                return;
            }

            Dictionary<string, string> activePurchasedProducts = await firebaseService.GetActivePurchasedProductsAsync();
            if (activePurchasedProducts == null)
            {
                return;
            }

            bool hasChanges = false;

            for (int i = purchasedProductIds.Count - 1; i >= 0; i--)
            {
                string productId = purchasedProductIds[i];
                if (!activePurchasedProducts.ContainsKey(productId))
                {
                    purchasedProductIds.RemoveAt(i);
                    hasChanges = true;
                }
            }

            var qrKeys = new List<string>(purchasedProductQRCodesById.Keys);
            foreach (string productId in qrKeys)
            {
                if (!activePurchasedProducts.TryGetValue(productId, out string activeQrPayload))
                {
                    purchasedProductQRCodesById.Remove(productId);
                    hasChanges = true;
                    continue;
                }

                if (!string.IsNullOrEmpty(activeQrPayload) &&
                    (!purchasedProductQRCodesById.TryGetValue(productId, out string localPayload) || localPayload != activeQrPayload))
                {
                    purchasedProductQRCodesById[productId] = activeQrPayload;
                    hasChanges = true;
                }
            }

            foreach (var activePurchasedProduct in activePurchasedProducts)
            {
                if (!purchasedProductIds.Contains(activePurchasedProduct.Key))
                {
                    purchasedProductIds.Add(activePurchasedProduct.Key);
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(activePurchasedProduct.Value))
                {
                    if (!purchasedProductQRCodesById.TryGetValue(activePurchasedProduct.Key, out string localPayload) ||
                        localPayload != activePurchasedProduct.Value)
                    {
                        purchasedProductQRCodesById[activePurchasedProduct.Key] = activePurchasedProduct.Value;
                        hasChanges = true;
                    }
                }
            }

            if (!hasChanges)
            {
                return;
            }

            SavePurchasedProducts();
        }

        private static async Task<FirestoreGameSecurityService> WaitForFirebaseServiceAsync()
        {
            const int maxAttempts = 100;

            for (int i = 0; i < maxAttempts; i++)
            {
                FirestoreGameSecurityService service = FirestoreGameSecurityService.Instance;
                if (service != null && service.IsReady)
                {
                    return service;
                }

                await Task.Delay(100);
            }

            return null;
        }

        public string RegisterPurchasedProduct(string productId, string qrPayload = null)
        {
            if (!purchasedProductIds.Contains(productId))
            {
                purchasedProductIds.Add(productId);

                if (!purchasedProductQRCodesById.ContainsKey(productId))
                {
                    purchasedProductQRCodesById[productId] = string.IsNullOrEmpty(qrPayload)
                        ? $"{productId}-{Guid.NewGuid():N}"
                        : qrPayload;
                }

                SavePurchasedProducts();
            }

            return productId;
        }


        private void SavePurchasedProducts()
        {
            var purchasedProducts = purchasedProductsRepo.Get();
            purchasedProducts.purchasedProductIds = purchasedProductIds;
            purchasedProducts.purchasedProductQRCodesById = purchasedProductQRCodesById;
            purchasedProductsRepo.Save(purchasedProducts);
        }

        public bool IsProductPurchased(string productId)
        {
            return purchasedProductIds.Contains(productId);
        }

        public string GetQRCodePayloadByProductId(string productId)
        {
            if (purchasedProductQRCodesById.TryGetValue(productId, out string qrPayload) && !string.IsNullOrEmpty(qrPayload))
            {
                return qrPayload;
            }

            return productId;
        }

        public ProductConfig GetProductConfigById(string Id)
        {
            return storeSettings.ProductConfigs.configs.Find(x => x.Id == Id);
        }

        public override void Tick()
        {
            
        }

        public override void Dispose()
        {
            
        }
    }
}
