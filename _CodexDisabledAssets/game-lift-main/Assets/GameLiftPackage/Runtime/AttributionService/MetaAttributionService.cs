using Cysharp.Threading.Tasks;
using Facebook.Unity;
using UnityEngine;

namespace GameLift.Attribution
{
    public class MetaAttributionService
    {
        private readonly UniTaskCompletionSource _initTcs = new UniTaskCompletionSource();

        /// <summary>
        /// Starts FB.Init and returns a UniTask that completes once ActivateApp has been called.
        /// Must be called AFTER the ATT prompt has been resolved.
        /// </summary>
        public UniTask InitializeAsync()
        {
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
        }

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

        public void LogPurchase(float amount, string currency)
        {
            if (!FB.IsInitialized)
            {
                Debug.LogWarning($"[MetaAttributionService] FB not initialized. Dropping LogPurchase: amount={amount}, currency={currency}");
                return;
            }
            Debug.Log($"[MetaAttributionService] LogPurchase: amount={amount}, currency={currency}");
            FB.LogPurchase(amount, currency);
        }

        public void LogEvent(string eventName)
        {
            if (!FB.IsInitialized)
            {
                Debug.LogWarning($"[MetaAttributionService] FB not initialized. Dropping LogEvent: {eventName}");
                return;
            }
            Debug.Log($"[MetaAttributionService] LogAppEvent: {eventName}");
            FB.LogAppEvent(eventName);
        }
    }
}
