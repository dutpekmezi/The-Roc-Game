using System.Collections.Generic;
using UnityEngine;

namespace GameLift.Purchasing
{
    [CreateAssetMenu(fileName = "PurchasingSettings", menuName = "Game Lift/Purchasing/PurchasingSettings")]
    public class PurchasingSettings : ScriptableObject
    {
        [field: SerializeField] public string Environment { get; private set; } = "production";
        [field: SerializeField] public List<ProductConfig> Products { get; private set; } = new();
    }
}
