using System.Collections.Generic;
using System.Linq;
using Game.Systems;
using UnityEngine;
using Utils.Currency;
using Utils.ObjectFlowAnimator;
using Utils.Scene;

namespace Game.UI
{
    public class MenuCurrencyRewardFlyer : MonoBehaviour
    {
        private Canvas menuCanvas;

        private void Awake()
        {
            if (GameState.Instance != null)
            {
                GameState.Instance.SetState(GameFlowState.Menu);
            }

            menuCanvas = GetComponent<Canvas>();
        }

        private void Start()
        {
            FlyPendingRewards();
        }

        private void FlyPendingRewards()
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

            CurrencyBar[] currencyBars = GetComponentsInChildren<CurrencyBar>(true);
            if (currencyBars.Length == 0)
            {
                return;
            }

            var playButton = FindObjectsOfType<SceneChangeButton>(true)
                .FirstOrDefault(button => button.SceneId == SceneKeys.GameScene);

            if (playButton == null)
            {
                return;
            }

            var startScreenPos = GetScreenPoint(playButton.GetComponent<RectTransform>());

            foreach (var currencyBar in currencyBars)
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
                    endScreenPosProvider: () => GetScreenPoint(currencyBar.IconRectTransform),
                    sprite: currencyConfig.currencySprite,
                    parent: menuCanvas != null ? menuCanvas.transform as RectTransform : null,
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

        private Vector2 GetScreenPoint(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return Vector2.zero;
            }

            Camera camera = null;

            if (menuCanvas != null && menuCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camera = menuCanvas.worldCamera != null ? menuCanvas.worldCamera : Camera.main;
            }

            return RectTransformUtility.WorldToScreenPoint(camera, rectTransform.position);
        }
    }
}
