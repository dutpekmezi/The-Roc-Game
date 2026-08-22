using System.Collections.Generic;
using UnityEngine;

namespace GameLift.Popup
{
    [CreateAssetMenu(fileName = "PopupSettings", menuName = "Game Lift/Popup Service/Popup Settings")]
    public class PopupSettings : ScriptableObject
    {
        public List<PopupBase> popupBases;
    }
}