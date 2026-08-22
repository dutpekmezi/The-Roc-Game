using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils.Popup;
using VContainer;

namespace Utils.Buttons
{
    public class OpenPopupButton : BaseButton
    {
        [Header("OpenPopupButton")]
        [SerializeField] private string popupId;

        public string PopupId => popupId;
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
            var popup = popupService.Get(popupId);
            if (popup != null)
                return;

            var _window = popupService.Create(popupId);
        }
    }
}
