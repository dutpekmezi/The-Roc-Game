using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLift.Attribution
{
    public class AttributionService : IAttributionService
    {
        private MetaAttributionService _metaAttributionService;

        public async UniTask InitializeAsync()
        {
            Debug.Log("[AttributionService] InitializeAsync started.");
            _metaAttributionService = new MetaAttributionService();
            await _metaAttributionService.InitializeAsync();
            Debug.Log("[AttributionService] InitializeAsync completed. Meta SDK is ready.");
        }

        public void LogPurchase(float amount, string currency)
        {
            Debug.Log($"[AttributionService] LogPurchase: amount={amount}, currency={currency}");
            _metaAttributionService.LogPurchase(amount, currency);
        }

        public void LogEvent(string eventName)
        {
            Debug.Log($"[AttributionService] LogEvent: {eventName}");
            _metaAttributionService.LogEvent(eventName);
        }
    }
}