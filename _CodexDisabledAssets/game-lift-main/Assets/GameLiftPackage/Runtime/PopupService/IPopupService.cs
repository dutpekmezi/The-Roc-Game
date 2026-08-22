namespace GameLift.Popup
{
    public interface IPopupService
    {
        T Create<T>(bool forceShow = false) where T : PopupBase;
        void Close<T>() where T : PopupBase;
        T Get<T>() where T : PopupBase;
        PopupBase Create(string popupId, bool forceShow = false);
        PopupBase Get(string popupId);
        void CloseActivePopup();
        void BringFront(PopupBase popup);

    }
}
