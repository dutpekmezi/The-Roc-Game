using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Systems;

namespace Game.UI
{
    public class SpinFrame : MonoBehaviour
    {
        [SerializeField] private Image frameImage;
        [SerializeField] private Image rewardImage;
        [SerializeField] private TextMeshProUGUI amountText;

        private RewardData rewardData;

        public RewardData RewardData => rewardData;
        public CollectableConfig RewardConfig => rewardData?.CollectableData;
        public string RewardName => rewardData?.Name ?? string.Empty;
        public int RewardAmount => rewardData?.Amount ?? 0;

        public RectTransform IconRectTransform => rewardImage.rectTransform;

        public void Initialize(RewardData reward)
        {
            rewardData = reward;
            var collectableData = reward?.CollectableData;

            if (collectableData == null)
            {
                if (rewardImage != null)
                {
                    rewardImage.sprite = null;
                    rewardImage.enabled = false;
                }

                if (amountText != null)
                {
                    amountText.text = string.Empty;
                }

                return;
            }

            gameObject.name = $"SpinFrame ({reward.Name})";

            if (frameImage != null)
            {
                frameImage.color = collectableData.Color;
            }

            if (rewardImage != null)
            {
                rewardImage.enabled = true;
                rewardImage.sprite = collectableData.Icon;
            }

            if (amountText != null)
            {
                amountText.text = reward.Amount.ToString();
            }
        }
    }
}
