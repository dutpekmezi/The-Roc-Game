using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameLift.Popup;
using VContainer;

namespace GameLift.Buttons
{
    public class OpenPopupButton : BaseButton
    {
        [Header("OpenPopupButton")]
        [SerializeField] private string popupId;
        private IPopupService _popupService;

        public string PopupId => popupId;

        [Inject]
        private void Construct(IPopupService popupService)
        {
            _popupService = popupService;
        }

        public override void BaseOnClick()
        {
            base.BaseOnClick();

            var popup = _popupService.Get(popupId);
            if (popup != null)
                return;

            var _window = _popupService.Create(popupId);
        }
    }
}