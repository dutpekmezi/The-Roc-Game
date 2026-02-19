using Game.Systems;
using UnityEngine;
using Utils.Buttons;
using Utils.Popup;

namespace Game.UI
{
    public class StoreMenuButton : BaseButton
    {
        [SerializeField] private ProductSection section;

        public override void BaseOnClick()
        {
            base.BaseOnClick();

            var popupService = PopupService.Instance;
            if (popupService != null && popupService.Get(StorePopUp.PopupKey) == null)
            {
                StorePopUp instance = (StorePopUp)popupService.Create(StorePopUp.PopupKey);
                instance.SelectSection(section);
            }
        }
    }
}