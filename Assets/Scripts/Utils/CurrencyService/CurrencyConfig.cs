using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using Utils.ObjectFlowAnimator;

namespace Utils.Currency
{
    [CreateAssetMenu(fileName = "CurrencyConfig", menuName = "Systems/Currencies/Currency Config", order = 1)]
    public class CurrencyConfig : ScriptableObject
    {
        [Dropdown("GetCurrencyIds")]
        public string currencyId;

        public Sprite currencySprite;

        public FlowParticle currencyUIPrefab;

        public DestinationActionData destinationActionData;

        private List<string> GetCurrencyIds()
        {
            return CurrencyIds.GetCurrencyIds();
        }
    }
}