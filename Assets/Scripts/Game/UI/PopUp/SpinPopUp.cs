using System.Collections;
using System.Collections.Generic;
using TMPro;
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
            if (spinRewards.Count == 0)
            {
                return;
            }

            List<Transform> frameTransforms = new();
            CollectSpinFrames(framesRoot, frameTransforms);

            for (int i = 0; i < frameTransforms.Count; i++)
            {
                var frame = frameTransforms[i];
                var reward = spinRewards[i % spinRewards.Count];
                ApplyFrame(frame, reward, rewardAmountPerSpin);
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
                float z = Mathf.LerpAngle(currentZ, targetZ, eased);

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

        private static void CollectSpinFrames(Transform root, List<Transform> result)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);

                if (child.name.StartsWith("SpinFrame"))
                {
                    result.Add(child);
                }

                CollectSpinFrames(child, result);
            }
        }

        private static void ApplyFrame(Transform frame, CollectableConfig reward, int amount)
        {
            if (frame == null || reward == null)
            {
                return;
            }

            var frameImage = frame.GetComponent<Image>();
            if (frameImage != null)
            {
                frameImage.color = reward.Color;
            }

            var allImages = frame.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < allImages.Length; i++)
            {
                if (allImages[i].transform == frame)
                {
                    continue;
                }

                allImages[i].sprite = reward.Icon;
                break;
            }

            var texts = frame.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (texts.Length > 0)
            {
                texts[0].text = amount.ToString();
            }

            if (texts.Length > 1)
            {
                texts[1].text = reward.Name;
            }
        }
    }
}
