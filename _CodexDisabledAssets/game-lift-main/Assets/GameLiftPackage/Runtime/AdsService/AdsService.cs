using System;
using System.Threading;
using GameLift.Save;
using Unity.Services.LevelPlay;
using UnityEngine;
using VContainer.Unity;

namespace GameLift.Ads
{
    public class AdsService : IStartable, IDisposable
    {
        private const string NoAdsBoughtSaveKey = "NoAdsBought";
        private AdsSettings _adsSettings;
        private readonly ISaveService _saveService;

        public RewardedAds RewardedAds { get; private set; }
        public InterstitialAds InterstitialAds { get; private set; }
        public BannerAds BannerAds { get; private set; }

        public AdsService(AdsSettings adsSettings, ISaveService saveService)
        {
            _adsSettings = adsSettings;
            _saveService = saveService;
            RewardedAds = new RewardedAds(_adsSettings);
            InterstitialAds = new InterstitialAds(_adsSettings);
            BannerAds = new BannerAds(_adsSettings);
        }

        public void Start()
        {
            Debug.Log("[Unity Ads] LevelPlay.ValidateIntegration");
            LevelPlay.ValidateIntegration();

            Debug.Log($"[Unity Ads] Unity version {LevelPlay.UnityVersion}");

            Debug.Log("[Unity Ads] Register initialization callbacks");
            LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
            LevelPlay.OnInitFailed += SdkInitializationFailedEvent;

            // SDK init
            Debug.Log("[Unity Ads] LevelPlay SDK initialization");
            LevelPlay.Init(_adsSettings.AppKey);
        }

        void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
        {
            Debug.Log("[Unity Ads]: I got SdkInitializationCompletedEvent with config: " + config);
            EnableAds();
        }

        void SdkInitializationFailedEvent(LevelPlayInitError error)
        {
            Debug.LogError("[Unity Ads]: I got SdkInitializationFailedEvent with error: " + error);
        }

        void EnableAds()
        {
            //Add ImpressionSuccess Event
            LevelPlay.OnImpressionDataReady += ImpressionDataReadyEvent;

            RewardedAds.Enable();
            InterstitialAds.Enable();
            BannerAds.Enable();
        }

        void ImpressionDataReadyEvent(LevelPlayImpressionData impressionData)
        {
            Debug.Log($"[Unity Ads] Received ImpressionDataReadyEvent ToString(): {impressionData}");
            Debug.Log($"[Unity Ads] Received ImpressionDataReadyEvent allData: {impressionData.AllData}");
        }

        public void Dispose()
        {
            LevelPlay.OnInitSuccess -= SdkInitializationCompletedEvent;
            LevelPlay.OnInitFailed -= SdkInitializationFailedEvent;
            LevelPlay.OnImpressionDataReady -= ImpressionDataReadyEvent;

            RewardedAds.Disable();
            InterstitialAds.Disable();
            BannerAds.Disable();
        }

        public void NoAdsBought()
        {
            _saveService.Raw.Save(NoAdsBoughtSaveKey, "true");
        }

        public bool HasNoAdsBought()
        {
            return _saveService.Raw.LoadBool(NoAdsBoughtSaveKey);
        }
    }
}