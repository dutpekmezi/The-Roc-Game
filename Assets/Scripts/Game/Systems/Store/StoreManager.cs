using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems
{
    public class StoreManager : BaseSystem
    {
        private StoreSettings storeSettings;
        public StoreSettings StoreSettings => storeSettings;

        private List<string> purchasedProductIds = new List<string>();
        public List<string> PurchasedProductIds => purchasedProductIds;
        public static StoreManager Instance { get; private set; }

        public StoreManager(StoreSettings storeSettings) 
        {
            if (Instance != null && Instance != this)
            {
                Instance.Dispose();
            }

            Instance = this;

            this.storeSettings = storeSettings;
        }

        public string RegisterPurchasedProduct(string productId)
        {
            if (!purchasedProductIds.Contains(productId))
            {
                purchasedProductIds.Add(productId);
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