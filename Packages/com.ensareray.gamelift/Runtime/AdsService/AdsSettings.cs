using UnityEngine;
namespace GameLift.Ads
{
    [CreateAssetMenu(fileName = "AdsSettings", menuName = "Game Lift/Ads/AdsSettings")]
    public class AdsSettings : ScriptableObject
    {
        [field: SerializeField, Header("Android")] public string androidInterstitialId { get; private set; }
        [field: SerializeField] public string androidRewardedId { get; private set; }
        [field: SerializeField] public string androidBannerId { get; private set; }
        [field: SerializeField] public string androidAppKey { get; private set; }

        [field: SerializeField, Header("iOS")] public string iOSInterstitialId { get; private set; }
        [field: SerializeField] public string iOSRewardedId { get; private set; }
        [field: SerializeField] public string iOSBannerId { get; private set; }
        [field: SerializeField] public string iOSAppKey { get; private set; }

        public string AppKey
        {
            get
            {
#if UNITY_ANDROID
                return androidAppKey;
#elif UNITY_IOS
                return iOSAppKey;
#else
                return "unexpected_platform";
#endif
            }
        }

        public string InterstitialAdUnitId
        {
            get
            {
#if UNITY_ANDROID
                return androidInterstitialId;
#elif UNITY_IOS
                return iOSInterstitialId;
#else
                return "unexpected_platform";
#endif
            }
        }

        public string RewardedAdUnitId
        {
            get
            {
#if UNITY_ANDROID
                return androidRewardedId;
#elif UNITY_IOS
                return iOSRewardedId;
#else
                return "unexpected_platform";
#endif
            }
        }

        public string BannerAdUnitId
        {
            get
            {
#if UNITY_ANDROID
                return androidBannerId;
#elif UNITY_IOS
                return iOSBannerId;
#else
                return "unexpected_platform";
#endif
            }
        }
    }
}