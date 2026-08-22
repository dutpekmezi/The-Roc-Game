using Utils.Popup;
using VContainer;

namespace Utils.Buttons
{
    public class ClosePopupButton : BaseButton
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
            popupService?.CloseActivePopup();
        }
    }
}
