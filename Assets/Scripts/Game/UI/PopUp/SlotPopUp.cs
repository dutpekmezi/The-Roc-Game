using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using Game.Systems;
using GameLift.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Currency;
using Utils.Popup;
using VContainer;

namespace Game.UI
{
    public class SlotPopUp : PopupBase
    {
        private const int RewardSlotCount = 3;
        private const string RewardGroupPath = "Panel/RewardGroup";
        private const string RewardImagePath = "Panel/RewardGroup/Image";
        private const string SlotRunningSoundName = "Slot_Running_Sound";
        private const string SlotOverSoundName = "Slot_Over_Sound";
        private const string SlotRewardSoundName = "Slot_Reward_Sound";
        private const string EnergyCurrencyId = "energy";
        private const string CostTextPath = "Panel/CostText/Text (TMP)";
        private const string CostIconPath = "Panel/CostText/CostIcon";
        private const string CloseButtonPath = "CloseButton";

        private static readonly string[] DefaultContentPaths =
        {
            "Panel/SlotPanel/Slot1_Viewport/Content",
            "Panel/SlotPanel/Slot2_Viewport/Content",
            "Panel/SlotPanel/Slot3_Viewport/Content"
        };

        public const string PopupKey = "slot_popup";
        public override string PopupId => PopupKey;

        [SerializeField] private SlotConfig slotConfig;
        [SerializeField] private Button spinButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject rewardGroup;
        [SerializeField] private Image rewardImage;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Image costIcon;
        [SerializeField] private CurrencyConfig energyCurrencyConfig;
        [SerializeField] private List<RectTransform> slotContents = new();

        private readonly List<ReelRuntime> reels = new();
        private readonly List<Tween> activeTweens = new();
        private ProductConfigs productConfigs;
        private IAudioService audioService;
        private ICurrencyService currencyService;
        private EnergyService energyService;
        private FirestoreGameSecurityService firestoreService;
        private Coroutine spinRoutine;
        private bool isSpinning;
        private bool isSpinCostRequestInProgress;
        private bool isSlotRunningSoundPlaying;

        [Inject]
        private void Construct(IObjectResolver objectResolver)
        {
            if (objectResolver != null && objectResolver.TryResolve<ProductConfigs>(out var resolvedProductConfigs))
            {
                Construct(resolvedProductConfigs);
            }

            if (objectResolver != null && objectResolver.TryResolve<IAudioService>(out var resolvedAudioService))
            {
                audioService = resolvedAudioService;
            }

            if (objectResolver != null && objectResolver.TryResolve<ICurrencyService>(out var resolvedCurrencyService))
            {
                currencyService = resolvedCurrencyService;
            }

            if (objectResolver != null && objectResolver.TryResolve<EnergyService>(out var resolvedEnergyService))
            {
                energyService = resolvedEnergyService;
            }

            if (objectResolver != null && objectResolver.TryResolve<FirestoreGameSecurityService>(out var resolvedFirestoreService))
            {
                firestoreService = resolvedFirestoreService;
            }
        }

        public void Construct(ProductConfigs productConfigs)
        {
            this.productConfigs = productConfigs;
        }

        protected override void Awake()
        {
            base.Awake();

            PostAppear += BuildSlots;
            SetRewardVisible(false, null);
            RefreshCostUi();

            if (spinButton != null)
            {
                spinButton.onClick.RemoveListener(OnSpinButtonClicked);
                spinButton.onClick.AddListener(OnSpinButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (spinButton != null)
            {
                spinButton.onClick.RemoveListener(OnSpinButtonClicked);
            }

            PostAppear -= BuildSlots;
            StopActiveSpin();
        }

        private void BuildSlots()
        {
            StopActiveSpin();
            SetRewardVisible(false, null);
            RefreshCostUi();
            reels.Clear();

            var products = GetProducts();
            if (products.Count == 0 || slotConfig == null || slotConfig.SlotItemPrefab == null)
            {
                Debug.LogWarning("[SlotPopUp] Slot items could not be created. ProductConfigs or SlotConfig is missing.");
                SetSpinButtonInteractable(false);
                return;
            }
            Canvas.ForceUpdateCanvases();

            for (int i = 0; i < slotContents.Count; i++)
            {
                var content = slotContents[i];
                if (content == null)
                {
                    continue;
                }

                ClearContent(content);
                ConfigureContentLayoutForSlotItemSize(content);

                var createdItems = new List<SlotItem>(products.Count);
                for (int productIndex = 0; productIndex < products.Count; productIndex++)
                {
                    var item = Instantiate(slotConfig.SlotItemPrefab, content);
                    ApplySlotItemSize(item);
                    item.Init(products[productIndex]);
                    createdItems.Add(item);
                }

                reels.Add(new ReelRuntime(
                    content,
                    createdItems,
                    HasReverseArrangement(content),
                    slotConfig.HasCustomSlotItemHeight));
            }

            Canvas.ForceUpdateCanvases();

            for (int i = 0; i < reels.Count; i++)
            {
                SetContentY(reels[i].Content, 0f);
            }

            SetSpinButtonInteractable(reels.Count > 0);
        }

        private async void OnSpinButtonClicked()
        {
            if (isSpinning || isSpinCostRequestInProgress || reels.Count == 0)
            {
                return;
            }

            isSpinCostRequestInProgress = true;
            SetSpinButtonInteractable(false);
            SetCloseButtonInteractable(false);

            bool canSpin = false;
            try
            {
                canSpin = await TryConsumeSpinCostAsync();
            }
            finally
            {
                isSpinCostRequestInProgress = false;
            }

            if (this == null)
            {
                return;
            }

            if (!canSpin)
            {
                SetSpinButtonInteractable(reels.Count > 0);
                SetCloseButtonInteractable(true);
                return;
            }

            spinRoutine = StartCoroutine(SpinRoutine());
        }

        private async Task<bool> TryConsumeSpinCostAsync()
        {
            if (slotConfig == null || slotConfig.SpinCostAmount <= 0)
            {
                return true;
            }

            if (slotConfig.SpinCurrencyType == SlotSpinCurrencyType.Energy)
            {
                var service = energyService ?? EnergyService.Instance;
                if (service == null)
                {
                    Debug.LogWarning("[SlotPopUp] EnergyService bulunamadi; slot spin baslatilamadi.");
                    NoEnergyPopUp.Show();
                    return false;
                }

                bool consumed = await service.TryConsumeSpinAsync(slotConfig.SpinCostAmount);
                if (!consumed)
                {
                    NoEnergyPopUp.Show();
                }

                return consumed;
            }

            string currencyId = GetSpinCurrencyId(slotConfig.SpinCurrencyType);
            var serviceCurrency = currencyService ?? CurrencyService.Instance;
            if (serviceCurrency == null)
            {
                Debug.LogWarning("[SlotPopUp] CurrencyService bulunamadi; slot spin baslatilamadi.");
                return false;
            }

            bool purchased = serviceCurrency.TryPurchase(currencyId, slotConfig.SpinCostAmount);
            if (!purchased)
            {
                Debug.LogWarning("[SlotPopUp] Slot spin icin yeterli currency yok. currency="
                                 + currencyId
                                 + ", amount="
                                 + slotConfig.SpinCostAmount);
            }

            return purchased;
        }

        private IEnumerator SpinRoutine()
        {
            isSpinning = true;
            SetSpinButtonInteractable(false);
            SetCloseButtonInteractable(false);
            SetRewardVisible(false, null);

            var targetIndexes = CreateTargetIndexes();
            PrepareReelArrangements(targetIndexes);

            PlaySlotRunningSound();

            int completedReels = 0;
            for (int i = 0; i < reels.Count; i++)
            {
                int reelIndex = i;
                StartCoroutine(SpinReelRoutine(
                    reels[reelIndex],
                    targetIndexes[reelIndex],
                    slotConfig.GetReelSettings(reelIndex),
                    () => completedReels++));
            }

            yield return new WaitUntil(() => completedReels >= reels.Count);

            StopSlotRunningSound();
            bool rewardGained = TryGainReward(targetIndexes);
            PlaySlotCompletedSound(rewardGained);

            spinRoutine = null;
            isSpinning = false;
            SetSpinButtonInteractable(true);
            SetCloseButtonInteractable(true);
        }

        private int[] CreateTargetIndexes()
        {
            var targetIndexes = new int[reels.Count];
            for (int i = 0; i < reels.Count; i++)
            {
                targetIndexes[i] = GetRandomItemIndex(reels[i]);
            }

            if (reels.Count == 0)
            {
                return targetIndexes;
            }

            ProductConfig firstProductConfig = GetProductConfigAt(0, targetIndexes[0]);
            if (reels.Count > 1)
            {
                bool shouldMatchSecondSlot = RollProbability(slotConfig != null
                    ? slotConfig.SecondSlotsSimilarityRate
                    : 0f);

                if (shouldMatchSecondSlot)
                {
                    targetIndexes[1] = GetMatchingProductIndex(reels[1], firstProductConfig, targetIndexes[1]);
                }
            }

            if (reels.Count > 2)
            {
                ProductConfig secondProductConfig = GetProductConfigAt(1, targetIndexes[1]);
                if (firstProductConfig != null && firstProductConfig == secondProductConfig)
                {
                    bool shouldMatchLastSlot = RollProbability(slotConfig != null
                        ? slotConfig.LastSlotSimilarityRate
                        : 0f);

                    if (shouldMatchLastSlot)
                    {
                        targetIndexes[2] = GetMatchingProductIndex(reels[2], firstProductConfig, targetIndexes[2]);
                    }
                }
                else
                {
                    bool shouldMatchFirstAndLastSlots = RollProbability(slotConfig != null
                        ? slotConfig.FirstAndLastSlotsSimilarityRate
                        : 0f);

                    targetIndexes[2] = shouldMatchFirstAndLastSlots
                        ? GetMatchingProductIndex(reels[2], firstProductConfig, targetIndexes[2])
                        : GetDifferentProductIndex(reels[2], firstProductConfig, targetIndexes[2]);
                }
            }

            return targetIndexes;
        }

        private void PrepareReelArrangements(int[] targetIndexes)
        {
            if (targetIndexes == null)
            {
                return;
            }

            for (int i = 0; i < reels.Count && i < targetIndexes.Length; i++)
            {
                targetIndexes[i] = PrepareReelArrangement(reels[i], targetIndexes[i]);
            }
        }

        private static int PrepareReelArrangement(ReelRuntime reel, int targetIndex)
        {
            if (!ShouldReverseArrangement(reel, targetIndex))
            {
                return targetIndex;
            }

            int itemCount = reel.Items.Count;
            reel.Items.Reverse();

            for (int i = 0; i < reel.Items.Count; i++)
            {
                if (reel.Items[i] != null)
                {
                    reel.Items[i].transform.SetSiblingIndex(i);
                }
            }

            SetContentY(reel.Content, 0f);
            Canvas.ForceUpdateCanvases();

            return itemCount - 1 - targetIndex;
        }

        private static bool ShouldReverseArrangement(ReelRuntime reel, int targetIndex)
        {
            int itemCount = reel?.Items != null ? reel.Items.Count : 0;
            if (itemCount <= 1 || targetIndex < 0 || targetIndex >= itemCount)
            {
                return false;
            }

            return GetVisualIndex(reel, targetIndex) <= (itemCount - 1) * 0.5f;
        }

        private static int GetVisualIndex(ReelRuntime reel, int itemIndex)
        {
            int itemCount = reel?.Items != null ? reel.Items.Count : 0;
            if (itemCount <= 0)
            {
                return itemIndex;
            }

            return reel.ReverseArrangement
                ? itemCount - 1 - itemIndex
                : itemIndex;
        }

        private bool TryGainReward(int[] targetIndexes)
        {
            if (targetIndexes == null || targetIndexes.Length < RewardSlotCount || reels.Count < RewardSlotCount)
            {
                return false;
            }

            ProductConfig matchedProductConfig = null;
            for (int i = 0; i < RewardSlotCount; i++)
            {
                SlotItem selectedItem = GetSelectedItem(i, targetIndexes[i]);
                if (selectedItem == null || selectedItem.ProductConfig == null)
                {
                    return false;
                }

                if (i == 0)
                {
                    matchedProductConfig = selectedItem.ProductConfig;
                    continue;
                }

                if (selectedItem.ProductConfig != matchedProductConfig)
                {
                    return false;
                }
            }

            GainReward(matchedProductConfig);
            return true;
        }

        private ProductConfig GetProductConfigAt(int reelIndex, int targetIndex)
        {
            return GetSelectedItem(reelIndex, targetIndex)?.ProductConfig;
        }

        private SlotItem GetSelectedItem(int reelIndex, int targetIndex)
        {
            if (reelIndex < 0 || reelIndex >= reels.Count)
            {
                return null;
            }

            var items = reels[reelIndex].Items;
            if (items == null || targetIndex < 0 || targetIndex >= items.Count)
            {
                return null;
            }

            return items[targetIndex];
        }

        private void GainReward(ProductConfig productConfig)
        {
            SetRewardVisible(true, productConfig);
            _ = RegisterSlotRewardProductAsync(productConfig);
        }

        private async Task RegisterSlotRewardProductAsync(ProductConfig productConfig)
        {
            if (productConfig == null || string.IsNullOrEmpty(productConfig.Id))
            {
                return;
            }

            var storeManager = StoreManager.Instance;
            if (storeManager == null)
            {
                Debug.LogWarning("[SlotPopUp] StoreManager bulunamadi; slot reward product cart'a eklenemedi.");
                return;
            }

            storeManager.RegisterPurchasedProduct(productConfig.Id);
            RefreshProductCartIfOpen();

            string qrPayload = null;
            FirestoreGameSecurityService firebase = firestoreService ?? FirestoreGameSecurityService.Instance;
            if (firebase != null)
            {
                try
                {
                    if (!firebase.IsReady)
                    {
                        await firebase.InitializeServiceAsync();
                    }

                    if (firebase.IsReady)
                    {
                        PurchaseResult rewardResult = await firebase.ClaimSlotProductRewardAsync(productConfig);
                        if (rewardResult.IsSuccess)
                        {
                            qrPayload = rewardResult.QrPayload;
                        }
                        else
                        {
                            Debug.LogWarning("[SlotPopUp] Slot reward Firebase QR kaydi basarisiz: " + rewardResult.Error);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SlotPopUp] Slot reward Firebase QR kaydi tamamlanamadi: " + e.Message);
                }
            }

            if (!string.IsNullOrEmpty(qrPayload))
            {
                storeManager.RegisterPurchasedProduct(productConfig.Id, qrPayload);
                RefreshProductCartIfOpen();
            }
        }

        private static void RefreshProductCartIfOpen()
        {
            var popupService = PopupService.Instance;
            ProductCartPopUp productCartPopUp = popupService != null
                ? popupService.Get<ProductCartPopUp>()
                : null;

            productCartPopUp?.RefreshProducts();
        }

        private void PlaySlotRunningSound()
        {
            audioService?.Play(SlotRunningSoundName);
            isSlotRunningSoundPlaying = true;
        }

        private void StopSlotRunningSound()
        {
            if (!isSlotRunningSoundPlaying)
            {
                return;
            }

            audioService?.Stop(SoundType.SFX);
            isSlotRunningSoundPlaying = false;
        }

        private void PlaySlotCompletedSound(bool rewardGained)
        {
            audioService?.Play(rewardGained ? SlotRewardSoundName : SlotOverSoundName);
        }

        private void SetRewardVisible(bool visible, ProductConfig productConfig)
        {
            if (rewardImage != null)
            {
                rewardImage.sprite = productConfig != null ? productConfig.Sprite : null;
                rewardImage.enabled = visible && rewardImage.sprite != null;
            }

            if (rewardGroup != null)
            {
                rewardGroup.SetActive(visible);
            }
        }

        private void RefreshCostUi()
        {
            EnsureCostUiReferences();

            if (costText != null)
            {
                costText.text = slotConfig != null
                    ? slotConfig.SpinCostAmount.ToString()
                    : string.Empty;
            }

            if (costIcon == null)
            {
                return;
            }

            Sprite costSprite = GetSpinCostSprite();
            costIcon.sprite = costSprite;
            costIcon.enabled = costSprite != null;
        }

        private void EnsureCostUiReferences()
        {
            if (costText == null)
            {
                costText = transform.Find(CostTextPath)?.GetComponent<TMP_Text>();
            }

            if (costIcon == null)
            {
                costIcon = transform.Find(CostIconPath)?.GetComponent<Image>();
            }
        }

        private Sprite GetSpinCostSprite()
        {
            if (slotConfig == null)
            {
                return null;
            }

            CurrencyConfig currencyConfig = GetSpinCostCurrencyConfig(slotConfig.SpinCurrencyType);
            return currencyConfig != null ? currencyConfig.currencySprite : null;
        }

        private CurrencyConfig GetSpinCostCurrencyConfig(SlotSpinCurrencyType currencyType)
        {
            if (currencyType == SlotSpinCurrencyType.Energy)
            {
                return energyCurrencyConfig != null
                    ? energyCurrencyConfig
                    : GetCurrencyConfig(EnergyCurrencyId);
            }

            return GetCurrencyConfig(GetSpinCurrencyId(currencyType));
        }

        private CurrencyConfig GetCurrencyConfig(string currencyId)
        {
            if (string.IsNullOrEmpty(currencyId))
            {
                return null;
            }

            var serviceCurrency = currencyService ?? CurrencyService.Instance;
            return serviceCurrency?.GetCurrencyConfig(currencyId);
        }

        private IEnumerator SpinReelRoutine(
            ReelRuntime reel,
            int targetIndex,
            SlotReelSpinSettings settings,
            Action onComplete)
        {
            if (settings.startDelay > 0f)
            {
                yield return new WaitForSeconds(settings.startDelay);
            }

            Canvas.ForceUpdateCanvases();

            float itemStride = GetItemStride(reel);
            float lastItemY = Mathf.Max(0f, itemStride * (reel.Items.Count - 1));

            for (int loopIndex = 0; loopIndex < settings.fullLoopCount; loopIndex++)
            {
                SetContentY(reel.Content, 0f);
                yield return TweenContentY(reel.Content, lastItemY, settings.fullLoopDuration, Ease.Linear);
            }

            SetContentY(reel.Content, 0f);
            float targetY = Mathf.Max(0f, itemStride * GetVisualIndex(reel, targetIndex));
            yield return TweenContentY(reel.Content, targetY, settings.settleDuration, Ease.OutCubic);

            SetContentY(reel.Content, targetY);
            onComplete?.Invoke();
        }

        private IEnumerator TweenContentY(RectTransform content, float targetY, float duration, Ease ease)
        {
            var tween = content
                .DOAnchorPosY(targetY, Mathf.Max(0.01f, duration))
                .SetEase(ease)
                .SetLink(gameObject);

            activeTweens.Add(tween);

            while (tween.IsActive() && tween.IsPlaying())
            {
                yield return null;
            }

            activeTweens.Remove(tween);
        }

        private List<ProductConfig> GetProducts()
        {
            productConfigs ??= StoreManager.Instance != null && StoreManager.Instance.StoreSettings != null
                ? StoreManager.Instance.StoreSettings.ProductConfigs
                : null;

            var products = new List<ProductConfig>();
            if (productConfigs == null || productConfigs.configs == null)
            {
                return products;
            }

            for (int i = 0; i < productConfigs.configs.Count; i++)
            {
                if (productConfigs.configs[i] != null)
                {
                    products.Add(productConfigs.configs[i]);
                }
            }

            return products;
        }

        private static int GetRandomItemIndex(ReelRuntime reel)
        {
            int itemCount = reel?.Items != null ? reel.Items.Count : 0;
            return itemCount > 0
                ? UnityEngine.Random.Range(0, itemCount)
                : 0;
        }

        private static int GetMatchingProductIndex(ReelRuntime reel, ProductConfig productConfig, int fallbackIndex)
        {
            if (productConfig == null || reel?.Items == null)
            {
                return fallbackIndex;
            }

            for (int i = 0; i < reel.Items.Count; i++)
            {
                if (reel.Items[i] != null && reel.Items[i].ProductConfig == productConfig)
                {
                    return i;
                }
            }

            return fallbackIndex;
        }

        private static int GetDifferentProductIndex(ReelRuntime reel, ProductConfig productConfig, int fallbackIndex)
        {
            if (productConfig == null || reel?.Items == null)
            {
                return fallbackIndex;
            }

            var differentIndexes = new List<int>();
            for (int i = 0; i < reel.Items.Count; i++)
            {
                if (reel.Items[i] != null && reel.Items[i].ProductConfig != productConfig)
                {
                    differentIndexes.Add(i);
                }
            }

            return differentIndexes.Count > 0
                ? differentIndexes[UnityEngine.Random.Range(0, differentIndexes.Count)]
                : fallbackIndex;
        }

        private static bool RollProbability(float probability)
        {
            probability = Mathf.Clamp01(probability);
            return probability >= 1f || probability > 0f && UnityEngine.Random.value < probability;
        }

        private static string GetSpinCurrencyId(SlotSpinCurrencyType currencyType)
        {
            return currencyType switch
            {
                SlotSpinCurrencyType.Coin => CurrencyIds.Coin,
                SlotSpinCurrencyType.Coffee => CurrencyIds.Coffee,
                SlotSpinCurrencyType.Matcha => CurrencyIds.Matcha,
                SlotSpinCurrencyType.Cookie => CurrencyIds.Cookie,
                _ => EnergyCurrencyId
            };
        }

        private static float GetItemStride(ReelRuntime reel)
        {
            if (reel == null)
            {
                return 0f;
            }

            if (reel.UseCustomItemHeight)
            {
                float itemHeight = GetFirstItemHeight(reel);
                if (itemHeight > 0f)
                {
                    return itemHeight + GetVerticalLayoutSpacing(reel.Content);
                }
            }

            float viewportHeight = GetViewportHeight(reel.Content);
            if (viewportHeight > 0f)
            {
                return viewportHeight;
            }

            int itemCount = reel.Items != null ? reel.Items.Count : 0;
            float contentHeight = reel.Content != null ? Mathf.Abs(reel.Content.rect.height) : 0f;
            return itemCount > 0 && contentHeight > 0f
                ? contentHeight / itemCount
                : 0f;
        }

        private static float GetFirstItemHeight(ReelRuntime reel)
        {
            if (reel?.Items == null || reel.Items.Count == 0 || reel.Items[0] == null)
            {
                return 0f;
            }

            var itemRect = reel.Items[0].transform as RectTransform;
            return itemRect != null
                ? Mathf.Max(Mathf.Abs(itemRect.rect.height), Mathf.Abs(itemRect.sizeDelta.y))
                : 0f;
        }

        private static float GetVerticalLayoutSpacing(RectTransform content)
        {
            var layoutGroup = content != null ? content.GetComponent<VerticalLayoutGroup>() : null;
            return layoutGroup != null ? layoutGroup.spacing : 0f;
        }

        private static float GetViewportHeight(RectTransform content)
        {
            var viewport = content != null ? content.parent as RectTransform : null;
            if (viewport == null)
            {
                return 0f;
            }

            return Mathf.Max(Mathf.Abs(viewport.rect.height), Mathf.Abs(viewport.sizeDelta.y));
        }

        private static bool HasReverseArrangement(RectTransform content)
        {
            var layoutGroup = content != null ? content.GetComponent<VerticalLayoutGroup>() : null;
            return layoutGroup != null && layoutGroup.reverseArrangement;
        }

        private void ConfigureContentLayoutForSlotItemSize(RectTransform content)
        {
            if (content == null || slotConfig == null)
            {
                return;
            }

            var layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                return;
            }

            if (slotConfig.HasCustomSlotItemWidth)
            {
                layoutGroup.childControlWidth = false;
                layoutGroup.childForceExpandWidth = false;
            }

            if (slotConfig.HasCustomSlotItemHeight)
            {
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandHeight = false;
            }
        }

        private void ApplySlotItemSize(SlotItem item)
        {
            if (item == null || slotConfig == null)
            {
                return;
            }

            var itemRect = item.transform as RectTransform;
            if (itemRect == null)
            {
                return;
            }

            Vector2 slotItemSize = slotConfig.SlotItemSize;
            if (slotConfig.HasCustomSlotItemWidth)
            {
                itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, slotItemSize.x);
            }

            if (slotConfig.HasCustomSlotItemHeight)
            {
                itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, slotItemSize.y);
            }
        }

        private static void ClearContent(RectTransform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i);
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private static void SetContentY(RectTransform content, float y)
        {
            if (content == null)
            {
                return;
            }

            var position = content.anchoredPosition;
            position.y = y;
            content.anchoredPosition = position;
        }

        private void SetSpinButtonInteractable(bool interactable)
        {
            if (spinButton != null)
            {
                spinButton.interactable = interactable;
            }
        }

        private void SetCloseButtonInteractable(bool interactable)
        {
            if (closeButton == null)
            {
                closeButton = transform.Find(CloseButtonPath)?.GetComponent<Button>();
            }

            if (closeButton != null)
            {
                closeButton.interactable = interactable;
            }
        }

        private void StopActiveSpin()
        {
            if (spinRoutine != null)
            {
                StopCoroutine(spinRoutine);
                spinRoutine = null;
            }

            for (int i = activeTweens.Count - 1; i >= 0; i--)
            {
                if (activeTweens[i] != null && activeTweens[i].IsActive())
                {
                    activeTweens[i].Kill();
                }
            }

            activeTweens.Clear();
            StopSlotRunningSound();
            isSpinning = false;
            SetCloseButtonInteractable(true);
        }

        private sealed class ReelRuntime
        {
            public ReelRuntime(
                RectTransform content,
                List<SlotItem> items,
                bool reverseArrangement,
                bool useCustomItemHeight)
            {
                Content = content;
                Items = items;
                ReverseArrangement = reverseArrangement;
                UseCustomItemHeight = useCustomItemHeight;
            }

            public RectTransform Content { get; }
            public List<SlotItem> Items { get; }
            public bool ReverseArrangement { get; }
            public bool UseCustomItemHeight { get; }
        }
    }
}
