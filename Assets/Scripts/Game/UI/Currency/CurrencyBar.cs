using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Currency;
using Utils.Signal;

namespace Game.UI
{
    public class CurrencyBar : MonoBehaviour
    {
        [SerializeField] private CurrencyConfig currencyConfig;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private RectTransform iconRectTransform;

        private bool subscribed;

        public CurrencyConfig CurrencyConfig => currencyConfig;
        public RectTransform IconRectTransform => iconRectTransform;

        public void SetCurrencyConfig(CurrencyConfig config)
        {
            currencyConfig = config;
            InitializeFromConfig();
        }

        private void Awake()
        {
            InitializeReferences();
            InitializeFromConfig();
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshAmount();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            InitializeReferences();
            InitializeFromConfig();
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            SignalBus.Get<OnCurrencyChangedUISignal>().Subscribe(HandleCurrencyChanged);
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            SignalBus.Get<OnCurrencyChangedUISignal>().Unsubscribe(HandleCurrencyChanged);
            subscribed = false;
        }

        private void HandleCurrencyChanged(string currencyId, int amount)
        {
            if (currencyConfig == null || currencyId != currencyConfig.currencyId)
            {
                return;
            }

            SetAmount(amount);
        }

        private void RefreshAmount()
        {
            if (currencyConfig == null || CurrencyService.Instance == null)
            {
                return;
            }

            SetAmount(CurrencyService.Instance.GetCurrencyForUI(currencyConfig.currencyId));
        }

        private void InitializeReferences()
        {
            if (iconImage == null)
            {
                var images = GetComponentsInChildren<Image>(true);
                for (int i = 0; i < images.Length; i++)
                {
                    if (images[i].gameObject != gameObject && images[i].gameObject.name.Contains("Icon"))
                    {
                        iconImage = images[i];
                        break;
                    }
                }

                if (iconImage == null)
                {
                    for (int i = 0; i < images.Length; i++)
                    {
                        if (images[i].gameObject != gameObject)
                        {
                            iconImage = images[i];
                            break;
                        }
                    }
                }

                if (iconImage == null && images.Length > 0)
                {
                    iconImage = images[0];
                }
            }

            if (amountText == null)
            {
                amountText = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private void InitializeFromConfig()
        {
            if (currencyConfig == null)
            {
                TryAssignConfigFromIcon();
            }

            if (currencyConfig == null)
            {
                return;
            }

            if (iconImage != null)
            {
                iconImage.sprite = currencyConfig.currencySprite;
            }

            RefreshAmount();
        }

        private void TryAssignConfigFromIcon()
        {
            if (iconImage == null || iconImage.sprite == null || CurrencyService.Instance == null)
            {
                return;
            }

            var configs = CurrencyService.Instance.Settings != null ? CurrencyService.Instance.Settings.currencyConfigs : null;
            if (configs == null)
            {
                return;
            }

            for (int i = 0; i < configs.Count; i++)
            {
                var config = configs[i];
                if (config != null && config.currencySprite == iconImage.sprite)
                {
                    currencyConfig = config;
                    return;
                }
            }
        }

        private void SetAmount(int amount)
        {
            if (amountText == null)
            {
                return;
            }

            amountText.text = amount.ToString();
        }
    }
}
