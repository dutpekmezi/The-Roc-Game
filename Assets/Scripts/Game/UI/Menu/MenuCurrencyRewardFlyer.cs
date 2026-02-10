using System.Collections;
using System.Collections.Generic;
using Game.Installers;
using Game.Systems;
using UnityEngine;
using UnityEngine.UI;
using Utils.Currency;
using Utils.ObjectFlowAnimator;
using Utils.Scene;

namespace Game.UI
{
    public class MenuCurrencyRewardFlyer : MonoBehaviour
    {
        [SerializeField] private RectTransform canvas;
        [SerializeField] private Camera cam;
        [SerializeField] private SceneChangeButton playButton;
        [SerializeField] private List<CurrencyBar> currencyBarList = new List<CurrencyBar>();

        private void Awake()
        {
            GameState.Instance.SetState(GameFlowState.Menu);
        }

        private void Start()
        {
            FlyRewards();
        }

        private void FlyRewards()
        {
            if (GameState.Instance == null)
            {
                return;
            }

            Dictionary<string, int> rewards = GameState.Instance.ConsumePendingCurrencyRewards();
            if (rewards.Count == 0)
            {
                return;
            }

            if (UIFlowAnimator.Instance == null)
            {
                return;
            }

            if (playButton == null)
            {
                return;
            }

            var startScreenPos = RectTransformUtility.WorldToScreenPoint(Camera.main ,playButton.Transform.position);

            foreach (var currencyBar in currencyBarList)
            {
                if (currencyBar == null)
                {
                    continue;
                }

                var currencyConfig = currencyBar.CurrencyConfig;
                if (currencyConfig == null)
                {
                    continue;
                }

                if (!rewards.TryGetValue(currencyConfig.currencyId, out var amount) || amount <= 0)
                {
                    continue;
                }

                if (currencyBar.IconRectTransform == null)
                {
                    continue;
                }

                if (CurrencyService.Instance != null)
                {
                    CurrencyService.Instance.AddFakeCurrency(currencyConfig.currencyId, -amount);
                }

                UIFlowAnimator.Instance.AddNewDestinationAction(
                    startScreenPos: startScreenPos,
                    endScreenPosProvider: () => RectTransformUtility.WorldToScreenPoint(cam, currencyBar.IconRectTransform.position),
                    sprite: currencyConfig.currencySprite,
                    parent: canvas,
                    particleCount: amount,
                    destinationActionData: currencyConfig.destinationActionData,
                    prefab: currencyConfig.currencyUIPrefab,
                    onReceivedItem: () =>
                    {
                        CurrencyService.Instance?.AddFakeCurrency(currencyConfig.currencyId, 1);
                    }
                );
            }
        }

        /*private Vector2 GetScreenPoint(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return Vector2.zero;
            }

            Camera camera = Camera.main;

            return RectTransformUtility.WorldToScreenPoint(camera, rectTransform.position);
        }*/
    }
}
