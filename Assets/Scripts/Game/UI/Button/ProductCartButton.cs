using Game.Systems;
using UnityEngine;
using Utils.Buttons;
using Utils.Popup;

namespace Game.UI
{
    public class ProductCartButton : BaseButton
    {
        public override void BaseOnClick()
        {
            base.BaseOnClick();

            var popupService = PopupService.Instance;
            if (popupService != null && popupService.Get(ProductCartPopUp.PopupKey) == null)
            {
                ProductCartPopUp instance = (ProductCartPopUp)popupService.Create(ProductCartPopUp.PopupKey);
            }
        }
    }
}