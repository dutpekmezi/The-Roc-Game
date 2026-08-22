using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

namespace GameLift.PrivacyConsent
{
    public class PrivacyConsentService : IPrivacyConsentService
    {
        public async UniTask<bool> RequestConsentAsync()
        {
            Debug.Log("[PrivacyConsentService] RequestConsentAsync started.");

#if UNITY_IOS
            var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
            Debug.Log($"[PrivacyConsentService] Current ATT status: {status}");

            if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                Debug.Log("[PrivacyConsentService] ATT not determined, showing prompt...");
                // RequestAuthorizationTracking has no callback — poll until resolved
                ATTrackingStatusBinding.RequestAuthorizationTracking();

                while (ATTrackingStatusBinding.GetAuthorizationTrackingStatus()
                       == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
                    await UniTask.Yield();
                }

                status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
                Debug.Log($"[PrivacyConsentService] User responded to ATT prompt: {status}");
            }

            bool authorized = status == ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED;
            Debug.Log($"[PrivacyConsentService] RequestConsentAsync completed. Result: {authorized}");
            return authorized;
#else
            Debug.Log("[PrivacyConsentService] Non-iOS platform, skipping ATT. Returning true.");
            return true;
#endif
        }
    }
}
