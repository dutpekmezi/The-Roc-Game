using Game.Systems;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils.Popup;

namespace Game.UI
{
    public class StorePopUp : PopupBase
    {
        public const string PopupKey = "store_menu";
        public override string PopupId => PopupKey;

        [SerializeField] private RectTransform productsParent;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private ProductSection selectedSection = ProductSection.Matcha;

        private List<ProductCard> displayingProducts = new List<ProductCard>();
        protected override void Awake()
        {
            base.Awake();
            PostAppear += DisplayProducts;
            PostDisappear += HideProducts;
        }

        private void DisplayProducts()
        {
            HideProducts();

            var settings = StoreManager.Instance.StoreSettings;

            var productList = settings.ProductConfigs.configs;

            for (int i = 0; i < productList.Count; i++)
            {
                if (productList[i] == null || productList[i].section != selectedSection)
                {
                    continue;
                }

                var instance = Instantiate(settings.ProductCardPrefab, productsParent);
                instance.Init(productList[i]);

                displayingProducts.Add(instance);
            }

            ApplySectionBackground(settings.ProductConfigs);
        }

        private void HideProducts()
        {
            for (int i = 0; i < displayingProducts.Count; i++)
            {
                if (displayingProducts[i] != null) Destroy(displayingProducts[i]);
            }

            displayingProducts.Clear();
        }

        public void SelectMatchaSection()
        {
            SelectSection(ProductSection.Matcha);
        }

        public void SelectCoffeeSection()
        {
            SelectSection(ProductSection.Coffee);
        }

        public void SelectDessertSection()
        {
            SelectSection(ProductSection.Dessert);
        }

        private void SelectSection(ProductSection section)
        {
            selectedSection = section;
            DisplayProducts();
        }

        private void ApplySectionBackground(ProductConfigs productConfigs)
        {
            if (backgroundImage == null || productConfigs == null)
            {
                return;
            }

            if (productConfigs.TryGetSectionColor(selectedSection, out var color))
            {
                backgroundImage.color = color;
            }
        }
    }
}
