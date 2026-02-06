using Game.Systems;
using System.Collections.Generic;
using UnityEngine;
using Utils.Currency;
using Utils.Popup;

namespace Game.UI
{
    public class GameOverPopup : PopupBase
    {
        public const string PopupKey = "game_over";
        [SerializeField] private List<CollectableBar> collectableBars = new();

        public override string PopupId => PopupKey;

        protected override void Awake()
        {
            base.Awake();
            CacheCollectableBars();
            PostAppear += FlyCollectedCollectablesToBars;
            PostAppear += HandleGameOverState;
        }

        private void CacheCollectableBars()
        {
            if (collectableBars == null || collectableBars.Count == 0)
            {
                collectableBars = new List<CollectableBar>(GetComponentsInChildren<CollectableBar>(true));
            }
        }

        private void FlyCollectedCollectablesToBars()
        {
            if (CollectableSystem.Instance == null)
            {
                return;
            }

            CacheCollectableBars();

            foreach (var collectableBar in collectableBars)
            {
                if (collectableBar == null)
                {
                    continue;
                }

                var config = collectableBar.CollectableConfig;
                if (config == null)
                {
                    continue;
                }

                if (!CollectableSystem.Instance.TryGetCollectedCount(config, out var count))
                {
                    continue;
                }

                if (collectableBar.IconRectTransform == null)
                {
                    continue;
                }

                var currentCount = 0;
                collectableBar.SetCount(currentCount);
                CollectableSystem.Instance.FlyCollectedCollectablesToScreenPosition(
                    config,
                    () => GetScreenPoint(collectableBar.IconRectTransform),
                    count,
                    onReceivedItem: () =>
                    {
                        currentCount = Mathf.Min(currentCount + 1, count);
                        collectableBar.SetCount(currentCount);
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

            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            Camera camera = null;

            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }

            return RectTransformUtility.WorldToScreenPoint(camera, rectTransform.position);
        }

        private void HandleGameOverState()
        {
            if (GameState.Instance != null)
            {
                GameState.Instance.SetState(GameFlowState.GameOver);
            }

            if (CollectableSystem.Instance == null || CurrencyService.Instance == null)
            {
                return;
            }

            var collectedCounts = CollectableSystem.Instance.GetCollectedCounts();
            if (collectedCounts == null || collectedCounts.Count == 0)
            {
                return;
            }

            var pendingRewards = new Dictionary<string, int>();

            foreach (var collected in collectedCounts)
            {
                if (collected.Key == null || collected.Value <= 0)
                {
                    continue;
                }

                var currencyConfig = CurrencyService.Instance.GetCurrencyConfig(collected.Key.Id);
                if (currencyConfig == null)
                {
                    continue;
                }

                if (pendingRewards.TryGetValue(currencyConfig.currencyId, out var currentAmount))
                {
                    pendingRewards[currencyConfig.currencyId] = currentAmount + collected.Value;
                }
                else
                {
                    pendingRewards[currencyConfig.currencyId] = collected.Value;
                }
            }

            if (pendingRewards.Count == 0)
            {
                return;
            }

            foreach (var reward in pendingRewards)
            {
                CurrencyService.Instance.ModifyCurrency(reward.Key, reward.Value);
            }

            GameState.Instance?.SetPendingCurrencyRewards(pendingRewards);
        }
    }
}
