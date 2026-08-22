using Cysharp.Threading.Tasks;
using UnityEngine;
namespace GameLift.PrivacyConsent
{
    public interface IPrivacyConsentService
    {
        UniTask<bool> RequestConsentAsync();
    }
}