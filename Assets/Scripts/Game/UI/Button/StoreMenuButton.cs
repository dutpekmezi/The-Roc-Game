using Game.Systems;
using UnityEngine;
using Utils.Buttons;
using Utils.Popup;
using VContainer;

namespace Game.UI
{
    public class StoreMenuButton : BaseButton
    {
        [SerializeField] private ProductSection section;
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
            if (popupService != null && popupService.Get(StorePopUp.PopupKey) == null)
            {
                StorePopUp instance = (StorePopUp)popupService.Create(StorePopUp.PopupKey);
                instance.SelectSection(section);
            }
        }
    }
}
