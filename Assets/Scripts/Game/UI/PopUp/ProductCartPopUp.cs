using Game.Systems;
using System.Collections.Generic;
using UnityEngine;
using Utils.Popup;

namespace Game.UI
{
    public class ProductCartPopUp : PopupBase
    {
        [SerializeField] private PurchasedProductCard purchasedProductCardPrefab;
        [SerializeField] private RectTransform productParent;

        public const string PopupKey = "product_cart";
        public override string PopupId => PopupKey;

        private readonly List<string> displayingCards = new List<string>();

        protected override void Awake()
        {
            base.Awake();

            PostAppear += DisplayProducts;
        }

        private void DisplayProducts()
        {
            var purchasedProductIds = StoreManager.Instance.PurchasedProductIds;

            if (purchasedProductIds == null)
            {
                return;
            }

            for (int i = 0; i < purchasedProductIds.Count; i++)
            {
                string productId = purchasedProductIds[i];
                if (displayingCards.Contains(productId))
                {
                    continue;
                }

                var productConfig = StoreManager.Instance.GetProductConfigById(productId);
                if (productConfig == null)
                {
                    continue;
                }

                var instance = Instantiate(purchasedProductCardPrefab, productParent);
                instance.Init(productConfig);
                displayingCards.Add(productId);
            }
        }
    }
}
