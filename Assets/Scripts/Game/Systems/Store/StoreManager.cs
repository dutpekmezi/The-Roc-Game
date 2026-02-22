using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems
{
    public class StoreManager : MonoBehaviour
    {
        [SerializeField] private StoreSettings storeSettings;
        public StoreSettings StoreSettings => storeSettings;

        private List<string> purchasedProductIds;
        public static StoreManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance);
            }

            Instance = this;
        }

        public string RegisterPurchasedProduct(string productId)
        {
            if (!string.IsNullOrEmpty(productId))
            {
                purchasedProductIds.Add(productId);
            }

            return productId;
        }
    }
}