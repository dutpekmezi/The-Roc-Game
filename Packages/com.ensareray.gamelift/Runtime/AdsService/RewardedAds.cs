using System;
using Cysharp.Threading.Tasks;
using Unity.Services.LevelPlay;
using UnityEngine;
namespace GameLift.Ads
{
    public class RewardedAds
    {
        private readonly AdsSettings _adsSettings;

        private LevelPlayRewardedAd _rewardedAd;
        private Action _onAdClosed;
        private Action _onUserEarnedReward;
        private bool _isRewardEarned;

        public RewardedAds(AdsSettings adsSettings)
        {
            _adsSettings = adsSettings;
        }

        internal void Enable()
        {
            _rewardedAd = new LevelPlayRewardedAd(_adsSettings.RewardedAdUnitId);

            // Register to Rewarded Video events
            _rewardedAd.OnAdLoaded += RewardedVideoOnLoadedEvent;
            _rewardedAd.OnAdLoadFailed += RewardedVideoOnAdLoadFailedEvent;
            _rewardedAd.OnAdDisplayed += RewardedVideoOnAdDisplayedEvent;
            _rewardedAd.OnAdDisplayFailed += RewardedVideoOnAdDisplayedFailedEvent;
            _rewardedAd.OnAdRewarded += RewardedVideoOnAdRewardedEvent;
            _rewardedAd.OnAdClicked += RewardedVideoOnAdClickedEvent;
            _rewardedAd.OnAdClosed += RewardedVideoOnAdClosedEvent;
            _rewardedAd.OnAdInfoChanged += RewardedVideoOnAdInfoChangedEvent;

            _rewardedAd.LoadAd();
        }

        public void Disable()
        {
            _rewardedAd?.DestroyAd();
        }

        public bool CanShowRewardedAd()
        {
            if (_rewardedAd != null)
                return _rewardedAd.IsAdReady();

            return false;
        }

        public void ShowRewardedAd(Action onUserEarnedReward, Action onAdClosed = null)
        {
            this._onUserEarnedReward = onUserEarnedReward;
            this._onAdClosed = onAdClosed;
            this._isRewardEarned = false;

            if (_rewardedAd != null && _rewardedAd.IsAdReady())
            {
                Debug.Log("[Unity Ads] Showing Rewarded Video Ad");
                _rewardedAd.ShowAd();
            }
            else
            {
                Debug.Log("[Unity Ads] LevelPlay Rewarded Video Ad is not ready");
            }
        }

        public void LoadRewardedAd()
        {
            _rewardedAd?.DestroyAd();

            _rewardedAd = new LevelPlayRewardedAd(_adsSettings.RewardedAdUnitId);

            _rewardedAd.OnAdLoaded += RewardedVideoOnLoadedEvent;
            _rewardedAd.OnAdLoadFailed += RewardedVideoOnAdLoadFailedEvent;
            _rewardedAd.OnAdDisplayed += RewardedVideoOnAdDisplayedEvent;
            _rewardedAd.OnAdDisplayFailed += RewardedVideoOnAdDisplayedFailedEvent;
            _rewardedAd.OnAdRewarded += RewardedVideoOnAdRewardedEvent;
            _rewardedAd.OnAdClicked += RewardedVideoOnAdClickedEvent;
            _rewardedAd.OnAdClosed += RewardedVideoOnAdClosedEvent;
            _rewardedAd.OnAdInfoChanged += RewardedVideoOnAdInfoChangedEvent;

            _rewardedAd.LoadAd();
        }

        void RewardedVideoOnLoadedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received RewardedVideoOnLoadedEvent With AdInfo: {adInfo}");
        }

        void RewardedVideoOnAdLoadFailedEvent(LevelPlayAdError error)
        {
            Debug.Log($"[Unity Ads] Received RewardedVideoOnAdLoadFailedEvent With Error: {error}");
        }

        void RewardedVideoOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received RewardedVideoOnAdDisplayedEvent With AdInfo: {adInfo}");
        }

        void RewardedVideoOnAdDisplayedFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            Debug.Log($"[Unity Ads] Received RewardedVideoOnAdDisplayedFailedEvent With Error: {error}");

            if (_onAdClosed != null)
            {
                _onAdClosed();
                _onAdClosed = null;
            }

            LoadRewardedAd();
        }

        void RewardedVideoOnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward reward)
        {
            Debug.Log($"[Unity Ads] Received RewardedVideoOnAdRewardedEvent With AdInfo: {adInfo} and Reward: {reward}");
            _isRewardEarned = true;
        }

        void RewardedVideoOnAdClickedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received RewardedVideoOnAdClickedEvent With AdInfo: {adInfo}");
        }

        async void RewardedVideoOnAdClosedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received RewardedVideoOnAdClosedEvent With AdInfo: {adInfo}");

            if (!_isRewardEarned)
            {
                // Wait one frame to allow a synchronous OnAdRewarded to fire (Editor mock fires it right after)
                await UniTask.Yield();
            }

            ResolveCallbacks();
        }

        void ResolveCallbacks()
        {
            if (_isRewardEarned)
            {
                _onUserEarnedReward?.Invoke();
            }
            else
            {
                _onAdClosed?.Invoke();
            }

            _onAdClosed = null;
            _onUserEarnedReward = null;
            _isRewardEarned = false;

            LoadRewardedAd();
        }

        void RewardedVideoOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received RewardedVideoOnAdInfoChangedEvent With AdInfo {adInfo}");
        }
    }
}