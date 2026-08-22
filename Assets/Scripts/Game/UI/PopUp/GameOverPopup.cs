using Game.Systems;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.Currency;
using Utils.Popup;
using VContainer;

namespace Game.UI
{
    public class GameOverPopup : PopupBase
    {
        [SerializeField] public const string PopupKey = "game_over";
        [SerializeField] private List<CollectableBar> collectableBars = new();
        [SerializeField] private float showDelay = 0.6f;

        public override string PopupId => PopupKey;
        protected override float ShowDelay => showDelay;

        private ICurrencyService _currencyService;
        private bool scoreRewardsApplied;

        [Inject]
        private void Construct(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        protected override void Awake()
        {
            base.Awake();
            CacheCollectableBars();
            PostAppear += ApplyScoreRewardsToCollectedCounts;
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

        private void ApplyScoreRewardsToCollectedCounts()
        {
            if (scoreRewardsApplied)
            {
                return;
            }

            scoreRewardsApplied = true;

            if (CollectableSystem.Instance == null || ScoreService.Instance == null)
            {
                return;
            }

            var scoreConfig = ScoreService.Instance.Config;
            if (scoreConfig == null)
            {
                return;
            }

            var scoreRewards = scoreConfig.CalculateCollectableRewards(ScoreService.Instance.RewardableScore);
            if (scoreRewards == null || scoreRewards.Count == 0)
            {
                return;
            }

            foreach (var scoreReward in scoreRewards)
            {
                CollectableSystem.Instance.AddCollectedCount(scoreReward.Key, scoreReward.Value);
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

        private async void HandleGameOverState()
        {
            var currencyService = _currencyService ?? CurrencyService.Instance;
            if (CollectableSystem.Instance == null || currencyService == null)
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

                var currencyConfig = currencyService.GetCurrencyConfig(collected.Key.Id);
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

            string runId = GameState.Instance != null
                ? GameState.Instance.ConsumeActiveRunId()
                : string.Empty;

            if (string.IsNullOrEmpty(runId))
            {
                Debug.LogWarning("[GameOverPopup] Server run id bulunamadi; odul claim edilmedi.");
#if UNITY_EDITOR
                Debug.LogWarning("[GameOverPopup] Editor fallback: Run odulu local-only veriliyor, Firebase'e yazmak icin functions deploy gerekli.");
                ApplyLocalRewards(currencyService, pendingRewards);
#endif
                return;
            }

            try
            {
                FirestoreGameSecurityService firebase = FirestoreGameSecurityService.Instance;
                if (firebase == null)
                {
                    Debug.LogWarning("[GameOverPopup] Firebase service yok; odul claim edilmedi.");
                    return;
                }

                Debug.Log("[GameOverPopup] Run reward claim basliyor. runId=" + runId + ", rewardTypes=" + pendingRewards.Count);
                RewardClaimResult claimResult = await firebase.ClaimRunRewardsAsync(runId, pendingRewards);
                if (!claimResult.IsSuccess)
                {
                    Debug.LogWarning("[GameOverPopup] Run reward claim reddedildi: " + claimResult.Error);
#if UNITY_EDITOR
                    Debug.LogWarning("[GameOverPopup] Editor fallback: Run odulu local-only veriliyor, Firebase'e yazmak icin functions deploy gerekli.");
                    ApplyLocalRewards(currencyService, pendingRewards);
#endif
                    return;
                }

                if (currencyService is CurrencyService concreteCurrencyService)
                {
                    await concreteCurrencyService.RefreshFromFirebaseAsync();
                }

                Debug.Log("[GameOverPopup] Run reward claim tamamlandi. runId=" + runId + ", grantTypes=" + (claimResult.Grants?.Count ?? 0));
                GameState.Instance?.AddPendingCurrencyRewards(claimResult.Grants);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameOverPopup] Run reward claim hata: " + e.Message);
#if UNITY_EDITOR
                Debug.LogWarning("[GameOverPopup] Editor fallback: Run odulu local-only veriliyor, Firebase'e yazmak icin functions deploy gerekli.");
                ApplyLocalRewards(currencyService, pendingRewards);
#endif
            }
        }

        private static void ApplyLocalRewards(
            ICurrencyService currencyService,
            Dictionary<string, int> rewards)
        {
            if (currencyService == null || rewards == null || rewards.Count == 0)
            {
                return;
            }

            foreach (var reward in rewards)
            {
                currencyService.ModifyCurrency(reward.Key, reward.Value);
            }

            GameState.Instance?.AddPendingCurrencyRewards(rewards);
        }
    }
}
