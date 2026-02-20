using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils.Currency;
using Utils.Popup;
using Game.Systems;
using Game.Installers;
using System;
using Utils.ObjectFlowAnimator;
using Utils.Pools;
using Utils.Logger;

namespace Game.UI
{
    public class SpinPopUp : PopupBase
    {
        public const string PopupKey = "spin_popup";
        public override string PopupId => PopupKey;

        [Header("Scene References")]
        [SerializeField] private Transform framesRoot;
        [SerializeField] private Transform wheelTransform;
        [SerializeField] private Button spinButton;
        [SerializeField] private List<SpinFrame> spinFrames = new();
        [SerializeField] private List<CurrencyBar> currencyBarList = new();

        [Header("Gameplay")]
        [SerializeField] private int rewardAmountPerSpin = 1;

        [Header("Spin Tuning")]
        [SerializeField] private float spinDuration = 2.5f;
        [SerializeField] private int minFullRotations = 4;
        [SerializeField] private int maxFullRotations = 6;

        [Header("Alignment")]
        [Tooltip("Pointer/ok hizası için derece offset. Örn: 0, 90, -90. Gerekirse dene.")]
        [SerializeField] private float angleOffset = 0f;

        [Tooltip("Prefab yönüne göre ters döndürmek gerekebilir.")]
        [SerializeField] private bool clockwise = false;

        private readonly List<CollectableConfig> spinRewards = new();
        private bool isSpinning;

        private const int DefaultPoolCapacity = 25;
        private const int DefaultPoolPreload = 1;
        private Pool rewardParticlePool;

        protected override void Awake()
        {
            base.Awake();

            PostAppear += RefreshFrames;
            EnsureSpinButton();

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
        }

        private void RefreshFrames()
        {
            if (framesRoot == null) framesRoot = transform;
            if (wheelTransform == null) wheelTransform = framesRoot;

            BuildSpinRewards();

            if (spinFrames == null || spinFrames.Count == 0) return;
            if (spinRewards.Count == 0) return;

            for (int i = 0; i < spinFrames.Count; i++)
            {
                var frame = spinFrames[i];
                if (frame == null) continue;

                var reward = spinRewards[i % spinRewards.Count];
                frame.Initialize(reward, rewardAmountPerSpin);
            }
        }

        private void OnSpinButtonClicked()
        {
            if (isSpinning) return;
            if (spinFrames == null || spinFrames.Count == 0) return;

            for (int i = 0; i < spinFrames.Count; i++)
            {
                if (spinFrames[i] != null && spinFrames[i].RewardConfig != null)
                {
                    StartCoroutine(SpinWheel());
                    return;
                }
            }
        }

        private IEnumerator SpinWheel()
        {
            wheelTransform.rotation = Quaternion.Euler(0, 0, 0);

            isSpinning = true;
            if (spinButton != null) spinButton.interactable = false;

            int segmentCount = spinFrames.Count;
            int selectedIndex = UnityEngine.Random.Range(0, segmentCount);

            var selectedFrame = spinFrames[selectedIndex];
            var reward = selectedFrame != null ? selectedFrame.RewardConfig : null;

            if (reward == null)
            {
                for (int i = 0; i < spinFrames.Count; i++)
                {
                    if (spinFrames[i] != null && spinFrames[i].RewardConfig != null)
                    {
                        reward = spinFrames[i].RewardConfig;
                        selectedIndex = i;
                        break;
                    }
                }
            }

            float frameAngle = 360f / segmentCount;
            float extraTurns = UnityEngine.Random.Range(minFullRotations, maxFullRotations + 1) * 360f;

            float currentZ = wheelTransform != null ? wheelTransform.localEulerAngles.z : 0f;

            float baseAngle = (frameAngle * selectedIndex);

            float direction = clockwise ? -1f : 1f;

            float targetZ = currentZ + extraTurns + (direction * baseAngle);

            float elapsed = 0f;
            while (elapsed < spinDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / spinDuration);

                float eased = 1f - Mathf.Pow(1f - t, 3f);

                float z = Mathf.Lerp(currentZ, targetZ, eased);

                if (wheelTransform != null)
                    wheelTransform.localRotation = Quaternion.Euler(0f, 0f, z);

                yield return null;
            }

            if (wheelTransform != null)
                wheelTransform.localRotation = Quaternion.Euler(0f, 0f, targetZ);

            GiveReward(reward);

            if (spinButton != null) spinButton.interactable = true;
            isSpinning = false;
        }

        private void GiveReward(CollectableConfig reward)
        {
            if (reward == null) return;
            if (CurrencyService.Instance == null) return;

            if (TryFlyRewardToCurrencyBar(reward)) return;

            CurrencyService.Instance.ModifyCurrency(reward.Id, rewardAmountPerSpin);
        }

        private bool TryFlyRewardToCurrencyBar(CollectableConfig reward)
        {
            if (reward == null || UIFlowAnimator.Instance == null) return false;
            if (!TryGetRewardStartScreenPosition(reward, out var startScreenPos)) return false;

            if (reward.Id == CollectableIds.Coin && MenuCurrencyRewardFlyer.Instance != null)
            {
                return MenuCurrencyRewardFlyer.Instance.FlyGold(startScreenPos, rewardAmountPerSpin);
            }

            var rewardCurrencyConfig = GetRewardCurrencyConfig(reward);
            if (rewardCurrencyConfig == null) return false;

            UIFlowAnimator.Instance.AddNewDestinationAction(
                startScreenPos: startScreenPos,
                endScreenPosProvider: () => GetRewardEndScreenPosition(reward, rewardCurrencyConfig, startScreenPos),
                sprite: rewardCurrencyConfig.currencySprite != null ? rewardCurrencyConfig.currencySprite : reward.Icon,
                parent: CoreInstaller.Instance.Canvas.transform as RectTransform,
                particleCount: rewardAmountPerSpin,
                destinationActionData: rewardCurrencyConfig.destinationActionData,
                prefab: rewardCurrencyConfig.currencyUIPrefab,
                onReceivedItem: () => CurrencyService.Instance?.ModifyCurrency(reward.Id, 1)
            );

            return true;
        }

        private CurrencyConfig GetRewardCurrencyConfig(CollectableConfig reward)
        {
            if (reward == null || CurrencyService.Instance == null || CurrencyService.Instance.Settings == null)
            {
                return null;
            }

            var currencyConfigs = CurrencyService.Instance.Settings.currencyConfigs;
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

        private bool TryGetRewardStartScreenPosition(CollectableConfig reward, out Vector3 startScreenPos)
        {
            startScreenPos = Vector3.zero;
            if (spinFrames == null || spinFrames.Count == 0) return false;

            for (int i = 0; i < spinFrames.Count; i++)
            {
                var frame = spinFrames[i];
                if (frame == null || frame.RewardConfig != reward) continue;

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

        private void BuildSpinRewards()
        {
            spinRewards.Clear();

            var collectableSystem = CollectableSystem.Instance;
            var collectableSettings = collectableSystem != null ? collectableSystem.CollectableSettings : null;
            if (collectableSettings?.collectablePrefabs == null) return;

            for (int i = 0; i < collectableSettings.collectablePrefabs.Count; i++)
            {
                var collectable = collectableSettings.collectablePrefabs[i];
                if (collectable == null || collectable.CollectableConfig == null) continue;

                var config = collectable.CollectableConfig;
                if (!spinRewards.Contains(config))
                    spinRewards.Add(config);
            }
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
    }
}
