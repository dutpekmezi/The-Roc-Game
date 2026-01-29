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
        private readonly Queue<PopupBase> _popupQueue = new();
        private PopupBase _activePopup;

        public T Create<T>() where T : PopupBase
        {
            var popupType = typeof(T);
            return (T)Create(popupType);
        }

        public void Close<T>() where T : PopupBase
        {
            if (_activePopup is T)
            {
                _activePopup.Disappear();
            }
        }

        public T Get<T>() where T : PopupBase
        {
            if (_activePopup != null && _activePopup is T)
            {
                return (T)_activePopup;
            }

            return null;
        }

        public PopupBase Create(string popupId)
        {
            var popupBase = _settings.popupBases.Find(x => x.PopupId == popupId);

            return Create(popupBase.GetType());
        }

        public PopupBase Get(string popupId)
        {
            if (_activePopup != null && _activePopup.PopupId == popupId)
                return _activePopup;

            return null;
        }

        public void CloseActivePopup()
        {
            if (_activePopup != null)
            {
                _activePopup.Disappear();
            }
        }

        private PopupBase Create(Type popupType)
        {
            var popupBase = _settings.popupBases.Find(x => x.GetType() == popupType);
            var instantiatedPopup = Instantiate(popupBase, transform);
            instantiatedPopup.transform.SetAsFirstSibling();

            if (_activePopup)
                _popupQueue.Enqueue(instantiatedPopup);
            else
                ShowPopup(instantiatedPopup);
            return instantiatedPopup;
        }

        private void ShowPopup(PopupBase popup)
        {
            _activePopup = popup;
            popup.Appear();
            popup.PostDisappear += HandleClosePopup;
        }

        private void HandleClosePopup()
        {
            _activePopup = null;

            if (_popupQueue.Count <= 0)
                return;

            var nextPopup = _popupQueue.Dequeue();
            ShowPopup(nextPopup);
        }

        [Serializable]
        public class Settings
        {
            public List<PopupBase> popupBases;
        }
    }
}