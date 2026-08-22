using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Utils.Currency;
using Utils.Popup;
using Game.Systems;
using Game.Installers;
using System;
using Utils.ObjectFlowAnimator;
using Utils.Pools;
using Utils.Logger;
using VContainer;
using GameLift.Audio;

namespace Game.UI
{
    public class SpinPopUp : PopupBase
    {
        private static readonly string[] SpinSegmentSoundNames =
        {
            "Lucky_Spin_1",
            "Lucky_Spin_2",
            "Lucky_Spin_3"
        };

        public const string PopupKey = "spin_popup";
        public override string PopupId => PopupKey;

        [Header("Scene References")]
        [SerializeField] private Transform framesRoot;
        [SerializeField] private Transform wheelTransform;
        [SerializeField] private Button spinButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private List<SpinFrame> spinFrames = new();
        [SerializeField] private List<CurrencyBar> currencyBarList = new();
        [SerializeField] private TMP_Text energyAmountText;
        [SerializeField] private GameObject nonEnergyText;

        [Header("Spin Tuning")]
        [SerializeField] private float spinDuration = 2.5f;
        [SerializeField] private int minFullRotations = 4;
        [SerializeField] private int maxFullRotations = 6;

        [Header("Alignment")]
        [Tooltip("Pointer/ok hizası için derece offset. Örn: 0, 90, -90. Gerekirse dene.")]
        [SerializeField] private float angleOffset = 0f;

        [Tooltip("Prefab yönüne göre ters döndürmek gerekebilir.")]
        [SerializeField] private bool clockwise = false;

        private bool isSpinning;

        private const int DefaultPoolCapacity = 25;
        private const int DefaultPoolPreload = 1;
        private Pool rewardParticlePool;
        private ICurrencyService _currencyService;
        private IUIFlowAnimator _uiFlowAnimator;
        private SpinRewardSystem _spinRewardSystem;
        private IAudioService _audioService;
        private EnergyService _energyService;
        private EnergyService _subscribedEnergyService;
        private bool isSpinRequestInProgress;

        [Inject]
        private void Construct(
            ICurrencyService currencyService,
            IUIFlowAnimator uiFlowAnimator,
            IAudioService audioService,
            EnergyService energyService)
        {
            _currencyService = currencyService;
            _uiFlowAnimator = uiFlowAnimator;
            _audioService = audioService;
            _energyService = energyService;
        }

        protected override void Awake()
        {
            base.Awake();

            PostAppear += RefreshFrames;
            PostAppear += RefreshEnergyUi;
            EnsureSpinButton();
            EnsureCloseButton();
            EnsureEnergyUiReferences();

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

            PostAppear -= RefreshFrames;
            PostAppear -= RefreshEnergyUi;
            UnsubscribeFromEnergyService();
        }

        private void RefreshFrames()
        {
            if (framesRoot == null) framesRoot = transform;
            if (wheelTransform == null) wheelTransform = framesRoot;

            if (spinFrames == null || spinFrames.Count == 0) return;

            _spinRewardSystem ??= SpinRewardSystem.Instance;

            for (int i = 0; i < spinFrames.Count; i++)
            {
                var frame = spinFrames[i];
                if (frame == null) continue;

                RewardData reward = null;
                _spinRewardSystem?.TryGetReward(i, out reward);
                frame.Initialize(reward);
            }
        }

        private void RefreshEnergyUi()
        {
            EnsureEnergyUiReferences();

            EnergyService energyService = _energyService ?? EnergyService.Instance;
            SubscribeToEnergyService(energyService);

            int energyAmount = energyService != null ? energyService.CurrentEnergy : 0;
            ApplyEnergyUi(energyAmount);
        }

        private void SubscribeToEnergyService(EnergyService energyService)
        {
            if (_subscribedEnergyService == energyService)
            {
                return;
            }

            UnsubscribeFromEnergyService();
            _subscribedEnergyService = energyService;

            if (_subscribedEnergyService != null)
            {
                _subscribedEnergyService.EnergyChanged += ApplyEnergyUi;
            }
        }

        private void UnsubscribeFromEnergyService()
        {
            if (_subscribedEnergyService != null)
            {
                _subscribedEnergyService.EnergyChanged -= ApplyEnergyUi;
                _subscribedEnergyService = null;
            }
        }

        private void ApplyEnergyUi(int energyAmount)
        {
            if (energyAmountText != null)
            {
                energyAmountText.text = $"ENERJİ : {energyAmount}";
            }

            if (nonEnergyText != null)
            {
                nonEnergyText.SetActive(energyAmount <= 0);
            }
        }

        private async void OnSpinButtonClicked()
        {
            if (isSpinning || isSpinRequestInProgress) return;
            if (spinFrames == null || spinFrames.Count == 0) return;

            for (int i = 0; i < spinFrames.Count; i++)
            {
                if (spinFrames[i] != null && spinFrames[i].RewardData?.CollectableData != null)
                {
                    isSpinRequestInProgress = true;
                    SetSpinControlsLocked(true);

                    EnergyService energyService = _energyService ?? EnergyService.Instance;
                    bool canSpin = false;
                    bool hasServerClaim = false;
                    SpinRewardClaimResult serverClaim = default;
                    try
                    {
                        FirestoreGameSecurityService firebase = FirestoreGameSecurityService.Instance;
                        if (firebase != null)
                        {
                            serverClaim = await firebase.ClaimSpinRewardAsync(spinFrames.Count);
                            canSpin = serverClaim.IsSuccess;
                            hasServerClaim = canSpin;

                            if (serverClaim.IsSuccess)
                            {
                                energyService?.ApplyServerEnergy(serverClaim.EnergyBalance);

                                if (_currencyService is CurrencyService concreteCurrencyService)
                                {
                                    await concreteCurrencyService.RefreshFromFirebaseAsync();
                                }
                            }
                            else if (serverClaim.IsInsufficient)
                            {
                                energyService?.ApplyServerEnergy(serverClaim.EnergyBalance);
                            }
#if UNITY_EDITOR
                            else
                            {
                                Debug.LogWarning("[SpinPopUp] Editor fallback: Spin server claim basarisiz, local-only enerjiyle deneniyor. Firebase'e yazmak icin functions deploy gerekli. Hata: " + serverClaim.Error);
                                canSpin = energyService != null &&
                                          await energyService.TryConsumeSpinAsync(syncCloud: false);
                            }
#endif
                        }
                        else
                        {
                            canSpin = energyService != null &&
                                      await energyService.TryConsumeSpinAsync();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[SpinPopUp] Spin enerji kontrolü tamamlanamadı: " + e.Message);
                    }
                    finally
                    {
                        isSpinRequestInProgress = false;
                    }

                    if (!canSpin)
                    {
                        SetSpinControlsLocked(false);
                        NoEnergyPopUp.Show();
                        return;
                    }

                    StartCoroutine(SpinWheel(hasServerClaim, serverClaim));
                    return;
                }
            }
        }

        private IEnumerator SpinWheel(bool hasServerClaim = false, SpinRewardClaimResult serverClaim = default)
        {
            wheelTransform.rotation = Quaternion.Euler(0, 0, 0);

            isSpinning = true;
            SetSpinControlsLocked(true);

            int segmentCount = spinFrames.Count;
            int selectedIndex = hasServerClaim
                ? Mathf.Clamp(serverClaim.SegmentIndex, 0, segmentCount - 1)
                : UnityEngine.Random.Range(0, segmentCount);

            var selectedFrame = spinFrames[selectedIndex];
            var reward = selectedFrame != null ? selectedFrame.RewardData : null;

            if (hasServerClaim &&
                !string.IsNullOrEmpty(serverClaim.CurrencyId) &&
                reward?.CollectableData?.Id != serverClaim.CurrencyId &&
                TryFindFrameByCurrency(serverClaim.CurrencyId, out int serverRewardIndex, out RewardData serverReward))
            {
                selectedIndex = serverRewardIndex;
                reward = serverReward;
            }

            if (reward?.CollectableData == null)
            {
                for (int i = 0; i < spinFrames.Count; i++)
                {
                    if (spinFrames[i] != null && spinFrames[i].RewardData?.CollectableData != null)
                    {
                        reward = spinFrames[i].RewardData;
                        selectedIndex = i;
                        break;
                    }
                }
            }

            int rewardAmount = hasServerClaim
                ? Mathf.Clamp(serverClaim.Amount, 0, int.MaxValue)
                : (reward != null ? reward.Amount : 0);

            float frameAngle = 360f / segmentCount;
            float extraTurns = UnityEngine.Random.Range(minFullRotations, maxFullRotations + 1) * 360f;

            float currentZ = wheelTransform != null ? wheelTransform.localEulerAngles.z : 0f;

            float baseAngle = (frameAngle * selectedIndex) + angleOffset;

            float direction = clockwise ? -1f : 1f;

            float targetZ = currentZ + extraTurns + (direction * baseAngle);

            float elapsed = 0f;
            int passedSegmentCount = 0;
            int spinSoundIndex = 0;

            while (elapsed < spinDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / spinDuration);

                float eased = 1f - Mathf.Pow(1f - t, 3f);

                float z = Mathf.Lerp(currentZ, targetZ, eased);

                if (wheelTransform != null)
                    wheelTransform.localRotation = Quaternion.Euler(0f, 0f, z);

                PlayPassedSegmentSounds(
                    Mathf.Abs(z - currentZ),
                    frameAngle,
                    ref passedSegmentCount,
                    ref spinSoundIndex);

                yield return null;
            }

            if (wheelTransform != null)
                wheelTransform.localRotation = Quaternion.Euler(0f, 0f, targetZ);

            PlayPassedSegmentSounds(
                Mathf.Abs(targetZ - currentZ),
                frameAngle,
                ref passedSegmentCount,
                ref spinSoundIndex);

            GiveReward(reward, hasServerClaim, rewardAmount);

            SetSpinControlsLocked(false);
            isSpinning = false;
        }

        private void PlayPassedSegmentSounds(
            float traveledAngle,
            float segmentAngle,
            ref int passedSegmentCount,
            ref int spinSoundIndex)
        {
            if (segmentAngle <= 0f || SpinSegmentSoundNames.Length == 0)
            {
                return;
            }

            int totalPassedSegments = Mathf.FloorToInt((traveledAngle + 0.001f) / segmentAngle);
            while (passedSegmentCount < totalPassedSegments)
            {
                _audioService?.Play(SpinSegmentSoundNames[spinSoundIndex]);

                spinSoundIndex = (spinSoundIndex + 1) % SpinSegmentSoundNames.Length;
                passedSegmentCount++;
            }
        }

        private bool TryFindFrameByCurrency(string currencyId, out int index, out RewardData reward)
        {
            index = -1;
            reward = null;

            if (string.IsNullOrEmpty(currencyId) || spinFrames == null)
            {
                return false;
            }

            for (int i = 0; i < spinFrames.Count; i++)
            {
                RewardData frameReward = spinFrames[i] != null ? spinFrames[i].RewardData : null;
                if (frameReward?.CollectableData?.Id != currencyId)
                {
                    continue;
                }

                index = i;
                reward = frameReward;
                return true;
            }

            return false;
        }

        private void GiveReward(RewardData reward, bool rewardAlreadyClaimed = false, int amountOverride = 0)
        {
            var collectableData = reward?.CollectableData;
            if (collectableData == null) return;
            var currencyService = _currencyService ?? CurrencyService.Instance;
            if (currencyService == null) return;

            int amount = amountOverride > 0 ? amountOverride : reward.Amount;
            if (amount <= 0) return;

            if (!rewardAlreadyClaimed)
            {
                currencyService.ModifyCurrency(collectableData.Id, amount);
            }

            TryFlyRewardToCurrencyBar(reward, rewardAlreadyApplied: true, amount);
        }

        private bool TryFlyRewardToCurrencyBar(RewardData reward, bool rewardAlreadyApplied, int amountOverride = 0)
        {
            var uiFlowAnimator = _uiFlowAnimator ?? UIFlowAnimator.Instance;
            var currencyService = _currencyService ?? CurrencyService.Instance;
            var collectableData = reward?.CollectableData;
            int amount = amountOverride > 0
                ? amountOverride
                : (reward != null ? reward.Amount : 0);

            if (collectableData == null || uiFlowAnimator == null || currencyService == null) return false;
            if (amount <= 0) return false;
            if (!TryGetRewardStartScreenPosition(reward, out var startScreenPos)) return false;

            if (collectableData.Id == CollectableIds.Coin && MenuCurrencyRewardFlyer.Instance != null)
            {
                return MenuCurrencyRewardFlyer.Instance.FlyGold(
                    startScreenPos,
                    amount,
                    rewardAlreadyApplied);
            }

            var rewardCurrencyConfig = GetRewardCurrencyConfig(collectableData);
            if (rewardCurrencyConfig == null) return false;

            if (rewardAlreadyApplied)
            {
                currencyService.AddFakeCurrency(collectableData.Id, -amount);
            }

            uiFlowAnimator.AddNewDestinationAction(
                startScreenPos: startScreenPos,
                endScreenPosProvider: () => GetRewardEndScreenPosition(collectableData, rewardCurrencyConfig, startScreenPos),
                sprite: rewardCurrencyConfig.currencySprite != null ? rewardCurrencyConfig.currencySprite : collectableData.Icon,
                parent: CoreInstaller.Instance.Canvas.transform as RectTransform,
                particleCount: amount,
                destinationActionData: rewardCurrencyConfig.destinationActionData,
                prefab: rewardCurrencyConfig.currencyUIPrefab,
                onReceivedItem: () =>
                {
                    if (rewardAlreadyApplied)
                    {
                        currencyService.AddFakeCurrency(collectableData.Id, 1);
                    }
                    else
                    {
                        currencyService.ModifyCurrency(collectableData.Id, 1);
                    }
                }
                // TEMP: Fly arrival sounds are disabled.
                // receivedSoundName: "Fly_Collectable"
            );

            return true;
        }

        private CurrencyConfig GetRewardCurrencyConfig(CollectableConfig reward)
        {
            var currencyService = _currencyService ?? CurrencyService.Instance;
            if (reward == null || currencyService == null || currencyService.Settings == null)
            {
                return null;
            }

            var currencyConfigs = currencyService.Settings.currencyConfigs;
            if (currencyConfigs == null)
            {
                return null;
            }

            for (int i = 0; i < currencyConfigs.Count; i++)
            {
                var currencyConfig = currencyConfigs[i];
                if (currencyConfig != null && currencyConfig.currencyId == reward.Id)
                {
                    return currencyConfig;
                }
            }

            return null;
        }

        private bool TryGetRewardStartScreenPosition(RewardData reward, out Vector3 startScreenPos)
        {
            startScreenPos = Vector3.zero;
            if (spinFrames == null || spinFrames.Count == 0) return false;

            for (int i = 0; i < spinFrames.Count; i++)
            {
                var frame = spinFrames[i];
                if (frame == null || frame.RewardData != reward) continue;

                if (frame.IconRectTransform != null)
                {
                    startScreenPos = GetScreenPoint(frame.IconRectTransform);
                    return true;
                }

                startScreenPos = Camera.main.WorldToScreenPoint(frame.transform.position);
                return true;
            }

            return false;
        }

        private Vector3 GetRewardEndScreenPosition(CollectableConfig reward, CurrencyConfig rewardCurrencyConfig, Vector3 fallback)
        {
            if (TryGetCurrencyBarScreenPosition(rewardCurrencyConfig, out var currencyBarScreenPos))
            {
                return currencyBarScreenPos;
            }

            if (GameCanvas.Instance != null && GameCanvas.Instance.TryGetCollectableBarScreenPosition(reward, out var barScreenPos))
            {
                return barScreenPos;
            }

            if (GameInstaller.Instance != null && GameInstaller.Instance.CollectableFlyDestination != null)
            {
                return GetScreenPoint(GameInstaller.Instance.CollectableFlyDestination);
            }

            return fallback;
        }

        private bool TryGetCurrencyBarScreenPosition(CurrencyConfig rewardCurrencyConfig, out Vector3 barScreenPos)
        {
            barScreenPos = Vector3.zero;
            if (rewardCurrencyConfig == null)
            {
                return false;
            }

            EnsureCurrencyBarList();
            if (currencyBarList == null || currencyBarList.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < currencyBarList.Count; i++)
            {
                var currencyBar = currencyBarList[i];
                if (currencyBar == null)
                {
                    continue;
                }

                var currencyConfig = currencyBar.CurrencyConfig;
                if (currencyConfig == null || currencyConfig.currencyId != rewardCurrencyConfig.currencyId)
                {
                    continue;
                }

                var iconRectTransform = currencyBar.IconRectTransform;
                if (iconRectTransform == null)
                {
                    continue;
                }

                barScreenPos = GetScreenPoint(iconRectTransform);
                return true;
            }

            return false;
        }

        private void EnsureCurrencyBarList()
        {
            if (currencyBarList != null && currencyBarList.Count > 0)
            {
                return;
            }

            var existingCurrencyBars = FindObjectsByType<CurrencyBar>(FindObjectsSortMode.None);
            if (existingCurrencyBars == null || existingCurrencyBars.Length == 0)
            {
                return;
            }

            if (currencyBarList == null)
            {
                currencyBarList = new List<CurrencyBar>();
            }

            for (int i = 0; i < existingCurrencyBars.Length; i++)
            {
                var currencyBar = existingCurrencyBars[i];
                if (currencyBar != null && !currencyBarList.Contains(currencyBar))
                {
                    currencyBarList.Add(currencyBar);
                }
            }
        }

        private Vector2 GetScreenPoint(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return Vector2.zero;
            }

            var targetCanvas = rectTransform.GetComponentInParent<Canvas>();
            Camera targetCamera = null;

            if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                targetCamera = targetCanvas.worldCamera != null ? targetCanvas.worldCamera : Camera.main;
            }

            return RectTransformUtility.WorldToScreenPoint(targetCamera, rectTransform.position);
        }

        private void EnsureSpinButton()
        {
            if (spinButton != null) return;

            var spinCenter = transform.Find("Panel/SpinPlack");
            if (spinCenter == null) return;

            spinButton = spinCenter.GetComponent<Button>();
            if (spinButton == null)
            {
                spinButton = spinCenter.gameObject.AddComponent<Button>();
                var image = spinCenter.GetComponent<Image>();
                if (image != null) image.raycastTarget = true;
            }
        }

        private void EnsureCloseButton()
        {
            if (closeButton != null) return;

            closeButton = transform.Find("Panel/CloseButton")?.GetComponent<Button>();
            if (closeButton != null) return;

            closeButton = transform.Find("CloseButton")?.GetComponent<Button>();
            if (closeButton != null) return;

            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == "CloseButton")
                {
                    closeButton = buttons[i];
                    return;
                }
            }
        }

        private void SetSpinControlsLocked(bool locked)
        {
            EnsureSpinButton();
            EnsureCloseButton();

            if (spinButton != null)
            {
                spinButton.interactable = !locked;
            }

            if (closeButton != null)
            {
                closeButton.interactable = !locked;
            }
        }

        private void EnsureEnergyUiReferences()
        {
            if (energyAmountText == null)
            {
                energyAmountText = transform.Find("Panel/EnergyAmountText")?.GetComponent<TMP_Text>();
            }

            if (nonEnergyText == null)
            {
                nonEnergyText = transform.Find("Panel/NonEnergyText")?.gameObject;
            }
        }
    }
}
