using GameLift.Popup;
using VContainer;

namespace GameLift.Buttons
{
    public class ClosePopupButton : BaseButton
    {
        private IPopupService _popupService;

        [Inject]
        private void Construct(IPopupService popupService)
        {
            _popupService = popupService;
        }
        public override void BaseOnClick()
        {
            base.BaseOnClick();

            _popupService.CloseActivePopup();
        }
    }
}