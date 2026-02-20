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
        [SerializeField] private TextMeshProUGUI rewardNameText;

        public void Initialize(CollectableConfig reward, int amount)
        {
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

            if (rewardNameText != null)
            {
                rewardNameText.text = reward.Name;
            }
        }
    }
}
