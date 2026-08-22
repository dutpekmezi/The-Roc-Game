using UnityEngine;
using Utils.Buttons;
using static System.Collections.Specialized.BitVector32;
using Utils.Popup;
using VContainer;

namespace Game.UI
{
    public class SpinWindowButton : BaseButton
    {
        private PopupService _popupService;

        [Inject]
        private void Construct(PopupService popupService)
        {
            _popupService = popupService;
        }

        public override void BaseOnClick()
        {
            base.BaseOnClick();

            var popupService = _popupService ?? PopupService.Instance;
            if (popupService != null && popupService.Get(SpinPopUp.PopupKey) == null)
            {
                SpinPopUp instance = (SpinPopUp)popupService.Create(SpinPopUp.PopupKey);
            }
        }
    }
}
