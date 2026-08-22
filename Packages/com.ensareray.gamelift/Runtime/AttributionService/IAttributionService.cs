using Cysharp.Threading.Tasks;

namespace GameLift.Attribution
{
    public interface IAttributionService
    {
        UniTask InitializeAsync();
        void LogPurchase(float amount, string currency);
        void LogEvent(string eventName);
    }
}