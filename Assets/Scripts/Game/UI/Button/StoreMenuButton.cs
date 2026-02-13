using UnityEngine;
using Utils.Buttons;
using Utils.Popup;

namespace Game.UI
{
    public class StoreMenuButton : BaseButton
    {
        public override void BaseOnClick()
        {
            base.BaseOnClick();

            var popupService = PopupService.Instance;
            if (popupService != null && popupService.Get(StorePopUp.PopupKey) == null)
            {
                popupService.Create(StorePopUp.PopupKey);
            }
        }
    }
}