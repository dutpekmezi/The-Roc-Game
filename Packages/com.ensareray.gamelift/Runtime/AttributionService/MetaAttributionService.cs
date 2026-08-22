using Cysharp.Threading.Tasks;
using UnityEngine;

#if FACEBOOK_UNITY
using Facebook.Unity;
#endif

namespace GameLift.Attribution
{
    public class MetaAttributionService
    {
#if FACEBOOK_UNITY
        private readonly UniTaskCompletionSource _initTcs = new UniTaskCompletionSource();
#endif

        /// <summary>
        /// Starts FB.Init and returns a UniTask that completes once ActivateApp has been called.
        /// Must be called AFTER the ATT prompt has been resolved.
        /// </summary>
        public UniTask InitializeAsync()
        {
#if FACEBOOK_UNITY
            Debug.Log("[MetaAttributionService] InitializeAsync called. FB.IsInitialized=" + FB.IsInitialized);

            if (!FB.IsInitialized)
            {
                Debug.Log("[MetaAttributionService] Starting FB.Init...");
                FB.Init(
                    onInitComplete: OnInitComplete,
                    onHideUnity: isGameShown =>
                    {
                        // Pause/resume the game when a Meta overlay (e.g. share dialog) appears
                        Time.timeScale = isGameShown ? 1 : 0;
                        Debug.Log($"[MetaAttributionService] onHideUnity — isGameShown={isGameShown}");
                    }
                );
            }
            else
            {
                Debug.Log("[MetaAttributionService] FB already initialized. Calling ActivateApp.");
                FB.ActivateApp();
                _initTcs.TrySetResult();
            }

            return _initTcs.Task;
#else
            Debug.Log("[MetaAttributionService] Facebook SDK is not installed. Attribution initialization skipped.");
            return UniTask.CompletedTask;
#endif
        }

#if FACEBOOK_UNITY
        private void OnInitComplete()
        {
            Debug.Log("[MetaAttributionService] FB.Init callback received. FB.IsInitialized=" + FB.IsInitialized);
            if (FB.IsInitialized)
            {
                Debug.Log("[MetaAttributionService] Calling ActivateApp.");
                FB.ActivateApp();
                _initTcs.TrySetResult();
            }
            else
            {
                Debug.LogError("[MetaAttributionService] FB.Init failed — FB.IsInitialized is still false. Attribution will be unavailable.");
                _initTcs.TrySetResult(); // Resolve gracefully so the game is never blocked
            }
        }
#endif

        public void LogPurchase(float amount, string currency)
        {
#if FACEBOOK_UNITY
            if (!FB.IsInitialized)
            {
                Debug.LogWarning($"[MetaAttributionService] FB not initialized. Dropping LogPurchase: amount={amount}, currency={currency}");
                return;
            }
            Debug.Log($"[MetaAttributionService] LogPurchase: amount={amount}, currency={currency}");
            FB.LogPurchase(amount, currency);
#else
            Debug.Log($"[MetaAttributionService] Facebook SDK is not installed. Dropping LogPurchase: amount={amount}, currency={currency}");
#endif
        }

        public void LogEvent(string eventName)
        {
#if FACEBOOK_UNITY
            if (!FB.IsInitialized)
            {
                Debug.LogWarning($"[MetaAttributionService] FB not initialized. Dropping LogEvent: {eventName}");
                return;
            }
            Debug.Log($"[MetaAttributionService] LogAppEvent: {eventName}");
            FB.LogAppEvent(eventName);
#else
            Debug.Log($"[MetaAttributionService] Facebook SDK is not installed. Dropping LogEvent: {eventName}");
#endif
        }
    }
}
