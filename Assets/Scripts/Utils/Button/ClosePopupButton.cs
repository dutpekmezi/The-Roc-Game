using Utils.Popup;

namespace Utils.Buttons
{
    public class ClosePopupButton : BaseButton
    {
        public override void BaseOnClick()
        {
            base.BaseOnClick();

            PopupService.Instance.CloseActivePopup();
        }
    }
}