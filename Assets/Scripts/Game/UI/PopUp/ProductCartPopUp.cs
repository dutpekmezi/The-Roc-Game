using Game.Systems;
using System.Collections.Generic;
using UnityEngine;
using Utils.Popup;
using VContainer;
using VContainer.Unity;

namespace Game.UI
{
    public class ProductCartPopUp : PopupBase
    {
        [SerializeField] private PurchasedProductCard purchasedProductCardPrefab;
        [SerializeField] private RectTransform productParent;

        public const string PopupKey = "product_cart";
        public override string PopupId => PopupKey;

        private readonly List<string> displayingCards = new List<string>();
        private IObjectResolver _resolver;

        [Inject]
        private void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        protected override void Awake()
        {
            base.Awake();

            PostAppear += DisplayProducts;
        }

        private void DisplayProducts()
        {
            var storeManager = StoreManager.Instance;
            if (storeManager == null)
            {
                return;
            }

            var purchasedProductIds = storeManager.PurchasedProductIds;

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

                var productConfig = storeManager.GetProductConfigById(productId);
                if (productConfig == null)
                {
                    continue;
                }

                var instance = Instantiate(purchasedProductCardPrefab, productParent);
                _resolver?.InjectGameObject(instance.gameObject);
                instance.Init(productConfig);
                displayingCards.Add(productId);
            }
        }

        public void RefreshProducts()
        {
            DisplayProducts();
        }
    }
}
