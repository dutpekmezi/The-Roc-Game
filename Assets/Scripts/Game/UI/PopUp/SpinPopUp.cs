using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils.Currency;
using Utils.Popup;
using Game.Systems;

namespace Game.UI
{
    public class SpinPopUp : PopupBase
    {
        public const string PopupKey = "spin_popup";
        public override string PopupId => PopupKey;

        [SerializeField] private Transform framesRoot;
        [SerializeField] private Transform wheelTransform;
        [SerializeField] private Button spinButton;
        [SerializeField] private List<SpinFrame> spinFrames = new();
        [SerializeField] private int rewardAmountPerSpin = 1;
        [SerializeField] private float spinDuration = 2.5f;
        [SerializeField] private int minFullRotations = 4;
        [SerializeField] private int maxFullRotations = 6;

        private readonly List<CollectableConfig> spinRewards = new();
        private bool isSpinning;

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
        }

        private void RefreshFrames()
        {
            if (framesRoot == null)
            {
                framesRoot = transform;
            }

            if (wheelTransform == null)
            {
                wheelTransform = framesRoot;
            }

            BuildSpinRewards();
            if (spinRewards.Count == 0 || spinFrames.Count == 0)
            {
                return;
            }

            for (int i = 0; i < spinFrames.Count; i++)
            {
                var frame = spinFrames[i];
                if (frame == null)
                {
                    continue;
                }

                var reward = spinRewards[i % spinRewards.Count];
                frame.Initialize(reward, rewardAmountPerSpin);
            }
        }

        private void OnSpinButtonClicked()
        {
            if (isSpinning || spinRewards.Count == 0)
            {
                return;
            }

            StartCoroutine(SpinWheel());
        }

        private IEnumerator SpinWheel()
        {
            isSpinning = true;
            if (spinButton != null)
            {
                spinButton.interactable = false;
            }

            var rewardIndex = Random.Range(0, spinRewards.Count);
            var reward = spinRewards[rewardIndex];

            var frameAngle = 360f / spinRewards.Count;
            var extraTurns = Random.Range(minFullRotations, maxFullRotations + 1) * 360f;
            var currentZ = wheelTransform != null ? wheelTransform.localEulerAngles.z : 0f;
            var targetZ = currentZ + extraTurns + (frameAngle * rewardIndex);

            float elapsed = 0f;
            while (elapsed < spinDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / spinDuration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                float z = Mathf.Lerp(currentZ, targetZ, eased);

                if (wheelTransform != null)
                {
                    wheelTransform.localRotation = Quaternion.Euler(0f, 0f, z);
                }

                yield return null;
            }

            if (wheelTransform != null)
            {
                wheelTransform.localRotation = Quaternion.Euler(0f, 0f, targetZ);
            }

            GiveReward(reward);

            if (spinButton != null)
            {
                spinButton.interactable = true;
            }

            isSpinning = false;
        }

        private void GiveReward(CollectableConfig reward)
        {
            if (reward == null || CurrencyService.Instance == null)
            {
                return;
            }

            CurrencyService.Instance.ModifyCurrency(reward.Id, rewardAmountPerSpin);
        }

        private void BuildSpinRewards()
        {
            spinRewards.Clear();

            var collectableSystem = CollectableSystem.Instance;
            var collectableSettings = collectableSystem != null ? collectableSystem.CollectableSettings : null;
            if (collectableSettings?.collectablePrefabs == null)
            {
                return;
            }

            for (int i = 0; i < collectableSettings.collectablePrefabs.Count; i++)
            {
                var collectable = collectableSettings.collectablePrefabs[i];
                if (collectable == null || collectable.CollectableConfig == null)
                {
                    continue;
                }

                var config = collectable.CollectableConfig;
                if (!spinRewards.Contains(config))
                {
                    spinRewards.Add(config);
                }
            }
        }

        private void EnsureSpinButton()
        {
            if (spinButton != null)
            {
                return;
            }

            var spinCenter = transform.Find("Panel/SpinPlack");
            if (spinCenter == null)
            {
                return;
            }

            spinButton = spinCenter.GetComponent<Button>();
            if (spinButton == null)
            {
                spinButton = spinCenter.gameObject.AddComponent<Button>();
                var image = spinCenter.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = true;
                }
            }
        }

    }
}
