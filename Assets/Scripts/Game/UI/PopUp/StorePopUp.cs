using Game.Systems;
using System.Collections.Generic;
using UnityEngine;
using Utils.Currency;
using Utils.Popup;

namespace Game.UI
{
    public class StorePopUp : PopupBase
    {
        public const string PopupKey = "store_menu";
        public override string PopupId => PopupKey;
        protected override void Awake()
        {
            base.Awake();
        }

    }
}
