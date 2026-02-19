using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Popup;
using Game.Systems;

namespace Game.UI
{
    public class SpinPopUp : PopupBase
    {
        public const string PopupKey = "spin_popup";
        public override string PopupId => PopupKey;

        [SerializeField] private Transform framesRoot;

        protected override void Awake()
        {
            base.Awake();
            PostAppear += RefreshFrames;
        }

        private void RefreshFrames()
        {
            if (framesRoot == null)
            {
                framesRoot = transform;
            }

            var rewardSystem = SpinRewardSystem.TryGetInstance();
            if (rewardSystem == null || rewardSystem.Rewards == null || rewardSystem.Rewards.Count == 0)
            {
                return;
            }

            List<Transform> spinFrames = new();
            CollectSpinFrames(framesRoot, spinFrames);

            for (int i = 0; i < spinFrames.Count; i++)
            {
                var frame = spinFrames[i];
                var reward = rewardSystem.Rewards[i % rewardSystem.Rewards.Count];

                ApplyFrame(frame, reward);
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

        private static void ApplyFrame(Transform frame, SpinRewardConfig reward)
        {
            if (frame == null || reward == null)
            {
                return;
            }

            var frameImage = frame.GetComponent<Image>();
            if (frameImage != null)
            {
                if (reward.Background != null)
                {
                    frameImage.sprite = reward.Background;
                }

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
                texts[0].text = reward.Amount.ToString();
            }

            if (texts.Length > 1)
            {
                texts[1].text = reward.RewardName;
            }
        }
    }
}
