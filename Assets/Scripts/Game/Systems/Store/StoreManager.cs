using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Utils.Save;

namespace Game.Systems
{
    public class PurchasedProductsEntity : ISaveable
    {
        public List<string> purchasedProductIds;

        public PurchasedProductsEntity()
        {
            purchasedProductIds = new List<string>();
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
            purchasedProductIds = purchasedProductsRepo.Get().purchasedProductIds;
        }

        public string RegisterPurchasedProduct(string productId)
        {
            if (!purchasedProductIds.Contains(productId))
            {
                purchasedProductIds.Add(productId);
                purchasedProductsRepo.Save(purchasedProductsRepo.Get());
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
