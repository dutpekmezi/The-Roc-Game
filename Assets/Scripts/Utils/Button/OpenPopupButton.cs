using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils.Popup;

namespace Utils.Buttons
{
    public class OpenPopupButton : BaseButton
    {
        [Header("OpenPopupButton")]
        [SerializeField] private string popupId;

        public string PopupId => popupId;

        public override void BaseOnClick()
        {
            base.BaseOnClick();

            var popup = PopupService.Instance.Get(popupId);
            if (popup != null)
                return;

            var _window = PopupService.Instance.Create(popupId);
        }
    }
}