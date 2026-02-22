using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.Singleton;

namespace Utils.Popup
{
    public class PopupService : Singleton<PopupService>
    {
        [SerializeField] private Settings _settings;
        private readonly List<PopupBase> _activePopups = new();

        public T Create<T>() where T : PopupBase
        {
            var popupType = typeof(T);
            return (T)Create(popupType);
        }

        public void Close<T>() where T : PopupBase
        {
            var popup = _activePopups.LastOrDefault(x => x is T);
            popup?.Disappear();
        }

        public T Get<T>() where T : PopupBase
        {
            return _activePopups.LastOrDefault(x => x is T) as T;
        }

        public PopupBase Create(string popupId)
        {
            var popupBase = _settings.popupBases.Find(x => x.PopupId == popupId);

            return Create(popupBase.GetType());
        }

        public PopupBase Get(string popupId)
        {
            return _activePopups.LastOrDefault(x => x.PopupId == popupId);
        }

        public void CloseActivePopup()
        {
            var popup = _activePopups.LastOrDefault();
            popup?.Disappear();
        }

        private PopupBase Create(Type popupType)
        {
            var popupBase = _settings.popupBases.Find(x => x.GetType() == popupType);
            var instantiatedPopup = Instantiate(popupBase, transform);
            ShowPopup(instantiatedPopup);
            return instantiatedPopup;
        }

        private void ShowPopup(PopupBase popup)
        {
            _activePopups.Add(popup);
            popup.Appear();
            popup.PostDisappear += () => HandleClosePopup(popup);
        }

        private void HandleClosePopup(PopupBase popup)
        {
            _activePopups.Remove(popup);
        }

        [Serializable]
        public class Settings
        {
            public List<PopupBase> popupBases;
        }
    }
}
