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
            StartCoroutine(FlyPendingRewardsAfterLayout());
        }

        private IEnumerator FlyPendingRewardsAfterLayout()
        {
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();

            if (canvas != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvas);
            }

            for (int i = 0; i < currencyBarList.Count; i++)
            {
                var currencyBar = currencyBarList[i];
                if (currencyBar == null)
                {
                    continue;
                }

                var rectTransform = currencyBar.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                }
            }

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

            if (playButton == null)
            {
                return;
            }

            var startScreenPos = RectTransformUtility.WorldToScreenPoint(GetUiCamera(), playButton.Transform.position);

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
                    endScreenPos: GetIconScreenPosition(currencyBar.IconRectTransform),
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

        private Vector2 GetIconScreenPosition(RectTransform iconRectTransform)
        {
            if (iconRectTransform == null)
            {
                return Vector2.zero;
            }

            var targetCamera = GetUiCamera();
            var worldPosition = iconRectTransform.TransformPoint(iconRectTransform.rect.center);
            return RectTransformUtility.WorldToScreenPoint(targetCamera, worldPosition);
        }

        private Camera GetUiCamera()
        {
            if (cam != null)
            {
                return cam;
            }

            var canvasComponent = canvas != null ? canvas.GetComponentInParent<Canvas>() : null;
            if (canvasComponent != null)
            {
                if (canvasComponent.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return null;
                }

                if (canvasComponent.worldCamera != null)
                {
                    return canvasComponent.worldCamera;
                }
            }

            return Camera.main;
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
