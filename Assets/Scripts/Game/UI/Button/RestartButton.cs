using Game.Installers;
using Game.Systems;
using Utils.Buttons;
using Utils.Popup;
using Utils.Scene;
using VContainer;

namespace Game.UI
{
    public class RestartButton : BaseButton
    {
        private bool _isRestarting;
        private ISceneService _sceneService;

        [Inject]
        private void Construct(ISceneService sceneService)
        {
            _sceneService = sceneService;
        }

        public override void BaseOnClick()
        {
            base.BaseOnClick();

            if (_isRestarting)
            {
                return;
            }

            _ = RestartGameScene();
        }

        private async System.Threading.Tasks.Task RestartGameScene()
        {
            _isRestarting = true;
            try
            {
                if (GameInstaller.Instance != null)
                {
                    if (await GameInstaller.Instance.RestartGameplay())
                    {
                        PopupService.Instance?.CloseActivePopup();
                    }

                    return;
                }

                if (RunGameInstaller.Instance != null)
                {
                    if (await RunGameInstaller.Instance.RestartGameplay())
                    {
                        PopupService.Instance?.CloseActivePopup();
                    }

                    return;
                }

                var sceneService = _sceneService ?? SceneService.Instance;
                if (sceneService != null)
                {
                    GameState.Instance?.RequestImmediateGameStart();
                    await sceneService.LoadScene(SceneKeys.GameScene);
                    PopupService.Instance?.CloseActivePopup();
                }
            }
            finally
            {
                _isRestarting = false;
            }
        }
    }
}
