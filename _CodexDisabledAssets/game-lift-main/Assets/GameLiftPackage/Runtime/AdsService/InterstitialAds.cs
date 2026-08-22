using System;
using Unity.Services.LevelPlay;
using UnityEngine;
namespace GameLift.Ads
{
    public class InterstitialAds
    {
        private readonly AdsSettings _adsSettings;

        private LevelPlayInterstitialAd _interstitialAd;
        private Action _onAdClosed;

        public InterstitialAds(AdsSettings adsSettings)
        {
            _adsSettings = adsSettings;
        }

        internal void Enable()
        {
            // Create Interstitial object
            _interstitialAd = new LevelPlayInterstitialAd(_adsSettings.InterstitialAdUnitId);

            // Register to Interstitial events
            _interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
            _interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
            _interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
            _interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
            _interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
            _interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
            _interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;

            _interstitialAd.LoadAd();
        }

        public void Disable()
        {
            _interstitialAd?.DestroyAd();
        }

        public bool CanShowInterstitialAd()
        {
            if (_interstitialAd == null) return false;

            return _interstitialAd.IsAdReady();
        }
        public void LoadInterstitialAd()
        {
            if (_interstitialAd == null)
            {
                _interstitialAd = new LevelPlayInterstitialAd(_adsSettings.InterstitialAdUnitId);

                _interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
                _interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
                _interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
                _interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
                _interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
                _interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
                _interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;
            }

            _interstitialAd.LoadAd();
        }

        public void ShowInterstitialAd(Action onAdClose = null)
        {
            if (_interstitialAd == null)
            {
                return;
            }

            this._onAdClosed = onAdClose;

            Debug.Log("[Unity Ads]: ShowInterstitialButtonClicked");
            if (_interstitialAd.IsAdReady())
            {
                _interstitialAd.ShowAd();
            }
            else
            {
                Debug.Log("[Unity Ads]: Levelplay Interstital Ad Ready? - False");
            }
        }

        void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received InterstitialOnAdLoadedEvent With AdInfo: {adInfo}");
        }

        void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error)
        {
            Debug.Log($"[Unity Ads] Received InterstitialOnAdLoadFailedEvent With Error: {error}");
        }

        void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received InterstitialOnAdDisplayedEvent With AdInfo: {adInfo}");
        }
        void InterstitialOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            Debug.Log($"[Unity Ads] Received InterstitialOnAdDisplayFailedEvent With InfoError: {error}");

            _onAdClosed?.Invoke();
            _onAdClosed = null;

            LoadInterstitialAd();
        }
        void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received InterstitialOnAdClickedEvent With AdInfo: {adInfo}");
        }

        void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received InterstitialOnAdClosedEvent With AdInfo: {adInfo}");

            _onAdClosed?.Invoke();
            _onAdClosed = null;

            LoadInterstitialAd();
        }

        void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received InterstitialOnAdInfoChangedEvent With AdInfo: {adInfo}");
        }

    }
}