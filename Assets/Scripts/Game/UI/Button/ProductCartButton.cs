using Game.Systems;
using UnityEngine;
using Utils.Buttons;
using Utils.Popup;
using VContainer;

namespace Game.UI
{
    public class ProductCartButton : BaseButton
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
            if (popupService != null && popupService.Get(ProductCartPopUp.PopupKey) == null)
            {
                ProductCartPopUp instance = (ProductCartPopUp)popupService.Create(ProductCartPopUp.PopupKey);
            }
        }
    }
}
