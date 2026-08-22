using Game.UI;
using UnityEngine;
using Utils.Buttons;
using Utils.Popup;
using VContainer;


public class SlotPopUpButton : BaseButton
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
        if (popupService != null && popupService.Get(SlotPopUp.PopupKey) == null)
        {
            SlotPopUp instance = (SlotPopUp)popupService.Create(SlotPopUp.PopupKey);
        }
    }
}
