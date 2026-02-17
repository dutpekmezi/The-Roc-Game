using Game.Systems;
using System.Collections.Generic;
using UnityEngine;
using Utils.Currency;
using Utils.Popup;

namespace Game.UI
{
    public class StorePopUp : PopupBase
    {
        public const string PopupKey = "store_menu";
        public override string PopupId => PopupKey;

        [SerializeField] private RectTransform productsParent;

        private List<ProductCard> displayingProducts = new List<ProductCard>();
        protected override void Awake()
        {
            base.Awake();
            PostAppear += DisplayProducts;
            PostDisappear += HideProducts;
        }

        private void DisplayProducts()
        {
            var settings = StoreManager.Instance.StoreSettings;

            var productList = settings.ProductConfigs.configs;

            for (int i = 0; i < productList.Count; i++)
            {
                var instance = Instantiate(settings.ProductCardPrefab, productsParent);
                instance.Init(productList[i]);

                displayingProducts.Add(instance);
            }
        }

        private void HideProducts()
        {
            for (int i = 0; i < displayingProducts.Count; i++)
            {
                if (displayingProducts[i] != null) Destroy(displayingProducts[i]);
            }

            displayingProducts.Clear();
        }
    }
}
