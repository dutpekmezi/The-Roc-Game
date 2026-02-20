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

        private CollectableConfig rewardConfig;
        public CollectableConfig RewardConfig => rewardConfig;

        public void Initialize(CollectableConfig reward, int amount)
        {
            rewardConfig = reward;

            if (reward == null)
            {
                return;
            }

            if (frameImage != null)
            {
                frameImage.color = reward.Color;
            }

            if (rewardImage != null)
            {
                rewardImage.sprite = reward.Icon;
            }

            if (amountText != null)
            {
                amountText.text = amount.ToString();
            }
        }
    }
}
