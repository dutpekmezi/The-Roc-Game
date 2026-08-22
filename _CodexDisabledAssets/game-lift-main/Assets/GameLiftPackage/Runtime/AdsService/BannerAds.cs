using Unity.Services.LevelPlay;
using UnityEngine;
namespace GameLift.Ads
{
    public class BannerAds
    {
        private readonly AdsSettings _adsSettings;

        private LevelPlayBannerAd _bannerAd;

        public BannerAds(AdsSettings adsSettings)
        {
            _adsSettings = adsSettings;
        }
        public void Enable()
        {

            _bannerAd = new LevelPlayBannerAd(_adsSettings.BannerAdUnitId);

            // Register to Banner events
            _bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
            _bannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
            _bannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;
            _bannerAd.OnAdDisplayFailed += BannerOnAdDisplayFailedEvent;
            _bannerAd.OnAdClicked += BannerOnAdClickedEvent;
            _bannerAd.OnAdCollapsed += BannerOnAdCollapsedEvent;
            _bannerAd.OnAdLeftApplication += BannerOnAdLeftApplicationEvent;
            _bannerAd.OnAdExpanded += BannerOnAdExpandedEvent;
        }

        public void Disable()
        {
            _bannerAd?.DestroyAd();
        }

        public void LoadBannerAd()
        {
            if (_bannerAd == null) return;

            Debug.Log("[Unity Ads]: loadBannerButtonClicked");
            _bannerAd.LoadAd();
        }

        public void HideBannerAd()
        {
            Debug.Log("[Unity Ads]: HideButtonClicked");
            _bannerAd.HideAd();
        }

        void BannerOnAdLoadedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received BannerOnAdLoadedEvent With AdInfo: {adInfo}");
        }

        void BannerOnAdLoadFailedEvent(LevelPlayAdError error)
        {
            Debug.Log($"[Unity Ads] Received BannerOnAdLoadFailedEvent With Error: {error}");
        }

        void BannerOnAdClickedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received BannerOnAdClickedEvent With AdInfo: {adInfo}");
        }

        void BannerOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received BannerOnAdDisplayedEvent With AdInfo: {adInfo}");
        }

        void BannerOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            Debug.Log($"[Unity Ads] Received BannerOnAdDisplayFailedEvent With AdInfo: {adInfo} and Error: {error}");
        }

        void BannerOnAdCollapsedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received BannerOnAdCollapsedEvent With AdInfo: {adInfo}");
        }

        void BannerOnAdLeftApplicationEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received BannerOnAdLeftApplicationEvent With AdInfo: {adInfo}");
        }

        void BannerOnAdExpandedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"[Unity Ads] Received BannerOnAdExpandedEvent With AdInfo: {adInfo}");
        }
    }
}