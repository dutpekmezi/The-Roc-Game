using Game.Systems;
using NUnit.Framework;
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

        private List<string> displayingCards = new List<string>();

        protected override void Awake()
        {
            base.Awake();

            PostAppear += DisplayProducts;
        }

        private void DisplayProducts()
        {
            var purchasedProductIds = StoreManager.Instance.PurchasedProductIds;

            if (purchasedProductIds != null)
            {
                for (int i = 0; i < purchasedProductIds.Count; i++)
                {
                    var productConfig = StoreManager.Instance.GetProductConfigById(purchasedProductIds[i]);

                    var instance = Instantiate(purchasedProductCardPrefab, productParent);
                    instance.Init(productConfig);
                }
            }
        }
    }
}