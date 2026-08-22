using Game.Installers;
using UnityEngine;
using Utils.Scene;
using Utils.Buttons;
using NaughtyAttributes;
using System.Collections.Generic;
using VContainer;
using Game.Systems;
using Utils.Popup;

namespace Game.UI
{
    public class SceneChangeButton : BaseButton
    {
        [SerializeField] private RectTransform _rectTransform;

        [SerializeField, Dropdown(("GetSceneKeys"))]
        private string sceneId;

        public string SceneId => sceneId;
        private ISceneService _sceneService;
        private bool _isLoading;

        [Inject]
        private void Construct(ISceneService sceneService)
        {
            _sceneService = sceneService;
        }

        public override async void BaseOnClick()
        {
            if (_isLoading || IsSceneChangeBlockedByHudTransition())
            {
                return;
            }

            _isLoading = true;
            base.BaseOnClick();

            try
            {
                if (sceneId == SceneKeys.MenuScene || sceneId == SceneKeys.MenuBaseScene)
                {
                    if (TryShowLoadedMenuWithoutReload())
                    {
                        return;
                    }
                }

                var sceneService = _sceneService ?? SceneService.Instance;
                if (sceneService == null)
                {
                    return;
                }

                if (sceneId == SceneKeys.StoreScene)
                {
                    await sceneService.RemoveScene(SceneKeys.MenuScene);
                }

                await sceneService.LoadScene(sceneId);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private bool IsSceneChangeBlockedByHudTransition()
        {
            var menuFlyer = MenuCurrencyRewardFlyer.Instance;
            return menuFlyer != null && menuFlyer.IsHudTransitionInProgress;
        }

        private bool TryShowLoadedMenuWithoutReload()
        {
            var menuFlyer = MenuCurrencyRewardFlyer.Instance;
            if (menuFlyer == null)
            {
                return false;
            }

            PopupService.Instance?.CloseActivePopup();

            if (GameInstaller.Instance != null)
            {
                GameInstaller.Instance.RestartToMenu();
                return true;
            }

            if (RunGameInstaller.Instance != null)
            {
                RunGameInstaller.Instance.RestartToMenu();
                return true;
            }

            var gameState = GameState.Instance;
            if (gameState == null || gameState.CurrentState == GameFlowState.Menu)
            {
                menuFlyer.ShowForMenu();
            }
            else
            {
                gameState.SetState(GameFlowState.Menu);
            }

            return true;
        }

        private List<string> GetSceneKeys()
        {
            return SceneKeys.GetValues();
        }

        public RectTransform Transform => _rectTransform;
    }
}
