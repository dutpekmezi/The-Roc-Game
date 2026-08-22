using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils.Singleton;
using VContainer;
using VContainer.Unity;

namespace Utils.Popup
{
    public class PopupService : Singleton<PopupService>
    {
        [SerializeField] private Settings _settings;
        private readonly List<PopupBase> _activePopups = new();
        private IObjectResolver _resolver;

        [Inject]
        private void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

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
            if (string.IsNullOrEmpty(popupId))
            {
                Debug.LogError("[PopupService] Cannot create popup: popupId is empty.");
                return null;
            }

            if (_settings == null || _settings.popupBases == null)
            {
                Debug.LogError($"[PopupService] Cannot create popup: settings are not ready. popupId={popupId}");
                return null;
            }

            var popupBase = _settings.popupBases.Find(
                x => x != null && x.PopupId == popupId);

            if (popupBase == null)
            {
                string registeredIds = string.Join(
                    ", ",
                    _settings.popupBases
                        .Where(x => x != null)
                        .Select(x => x.PopupId));

                Debug.LogError(
                    $"[PopupService] Popup is not registered. popupId={popupId}, registered=[{registeredIds}]");
                return null;
            }

            return CreateFromPrefab(popupBase);
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
            if (popupType == null || _settings == null || _settings.popupBases == null)
            {
                Debug.LogError("[PopupService] Cannot create popup: type or settings are invalid.");
                return null;
            }

            var popupBase = _settings.popupBases.Find(
                x => x != null && x.GetType() == popupType);

            if (popupBase == null)
            {
                Debug.LogError($"[PopupService] Popup type is not registered: {popupType.FullName}");
                return null;
            }

            return CreateFromPrefab(popupBase);
        }

        private PopupBase CreateFromPrefab(PopupBase popupBase)
        {
            var instantiatedPopup = Instantiate(popupBase, transform);
            _resolver?.InjectGameObject(instantiatedPopup.gameObject);
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
