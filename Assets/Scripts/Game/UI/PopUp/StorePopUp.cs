using Game.Systems;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils.Popup;
using VContainer;
using VContainer.Unity;

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
            PostDisappear += ClearProducts;
        }

        private void DisplayProducts()
        {
            ClearProducts();

            var storeManager = StoreManager.Instance;
            if (storeManager == null)
            {
                return;
            }

            var settings = storeManager.StoreSettings;

            var productList = settings.ProductConfigs.configs;

            for (int i = 0; i < productList.Count; i++)
            {
                if (productList[i] == null || productList[i].section != selectedSection)
                {
                    continue;
                }

                var instance = Instantiate(settings.ProductCardPrefab, productsParent);
                _resolver?.InjectGameObject(instance.gameObject);
                instance.Init(productList[i]);

                displayingProducts.Add(instance);
            }

            ApplySectionBackground(settings.ProductConfigs);
        }

        private void ClearProducts()
        {
            for (int i = 0; i < displayingProducts.Count; i++)
            {
                if (displayingProducts[i] != null)
                {
                    displayingProducts[i].gameObject.SetActive(false);
                    Destroy(displayingProducts[i].gameObject);
                }
            }

            displayingProducts.Clear();
        }

        public void SelectSection(ProductSection section)
        {
            selectedSection = section;
            DisplayProducts();
        }

        private void OnDestroy()
        {
            PostAppear -= DisplayProducts;
            PostDisappear -= ClearProducts;
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
