using UnityEngine;
using Utils.Buttons;
using static System.Collections.Specialized.BitVector32;
using Utils.Popup;

namespace Game.UI
{
    public class SpinWindowButton : BaseButton
    {
        public override void BaseOnClick()
        {
            base.BaseOnClick();

            var popupService = PopupService.Instance;
            if (popupService != null && popupService.Get(SpinPopUp.PopupKey) == null)
            {
                SpinPopUp instance = (SpinPopUp)popupService.Create(SpinPopUp.PopupKey);
            }
        }
    }
}