using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;

namespace GameLift.Popup
{
    public class PopupService : IPopupService
    {
        private PopupSettings _popupSettings;
        private readonly Transform _parent;
        private readonly IObjectResolver _objectResolver;

        private readonly Queue<PopupBase> _popupQueue = new();
        private PopupBase _activePopup;
        private readonly Stack<PopupBase> _suspendedPopups = new();

        public PopupService(PopupSettings popupSettings, Canvas popupCanvas, IObjectResolver objectResolver)
        {
            _popupSettings = popupSettings;
            _parent = popupCanvas.transform;
            _objectResolver = objectResolver;
        }

        public T Create<T>(bool forceShow = false) where T : PopupBase
        {
            var popupType = typeof(T);
            return (T)Create(popupType, forceShow);
        }

        public void Close<T>() where T : PopupBase
        {
            if (_activePopup is T)
            {
                _activePopup.Disappear();
                return;
            }

            // Also check the queue for matching popups
            RemoveFromQueue<T>();
        }

        public T Get<T>() where T : PopupBase
        {
            if (_activePopup != null && _activePopup is T)
            {
                return (T)_activePopup;
            }

            return null;
        }

        public PopupBase Create(string popupId, bool forceShow = false)
        {
            var popupBase = _popupSettings.popupBases.Find(x => x.PopupId == popupId);

            return Create(popupBase.GetType(), forceShow);
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

        private PopupBase Create(Type popupType, bool forceShow = false)
        {
            var popupBase = _popupSettings.popupBases.Find(x => x.GetType() == popupType);
            var instantiatedPopup = GameObject.Instantiate(popupBase, _parent);
            _objectResolver.Inject(instantiatedPopup);
            instantiatedPopup.transform.SetAsFirstSibling();

            if (forceShow)
            {
                // Suspend the current active popup so it can be restored later
                if (_activePopup != null)
                {
                    _suspendedPopups.Push(_activePopup);
                }

                ShowPopup(instantiatedPopup);
                BringFront(instantiatedPopup);
                return instantiatedPopup;
            }

            if (_activePopup)
            {
                // Hide the queued popup until it's its turn
                HideQueuedPopup(instantiatedPopup);
                _popupQueue.Enqueue(instantiatedPopup);
            }
            else
            {
                ShowPopup(instantiatedPopup);
            }

            return instantiatedPopup;
        }

        private void ShowPopup(PopupBase popup)
        {
            _activePopup = popup;
            popup.Appear();
            popup.PostDisappear += () => HandleClosePopup(popup);
        }

        private void HandleClosePopup(PopupBase closedPopup)
        {
            // Only clear active popup if the closed popup is actually the active one
            if (_activePopup == closedPopup)
            {
                _activePopup = null;

                // Restore the most recent suspended popup if one exists
                if (_suspendedPopups.Count > 0)
                {
                    _activePopup = _suspendedPopups.Pop();
                    return;
                }

                if (_popupQueue.Count <= 0)
                    return;

                var nextPopup = _popupQueue.Dequeue();
                ShowPopup(nextPopup);
            }
        }

        private void HideQueuedPopup(PopupBase popup)
        {
            var canvasGroup = popup.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        private void RemoveFromQueue<T>() where T : PopupBase
        {
            var tempQueue = new Queue<PopupBase>();
            while (_popupQueue.Count > 0)
            {
                var popup = _popupQueue.Dequeue();
                if (popup is T)
                {
                    // Destroy the removed popup
                    GameObject.Destroy(popup.gameObject);
                }
                else
                {
                    tempQueue.Enqueue(popup);
                }
            }

            while (tempQueue.Count > 0)
            {
                _popupQueue.Enqueue(tempQueue.Dequeue());
            }
        }

        public void BringFront(PopupBase popup)
        {
            if (popup == null) return;

            popup.transform.SetAsLastSibling();
        }
    }
}
