using Game.Installers;
using Utils.Buttons;
using Utils.Scene;

namespace Game.UI
{
    public class RestartButton : BaseButton
    {
        private bool _isRestarting;

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
                GameInstaller.Instance.Clear();

                await SceneService.Instance.RemoveScene(SceneKeys.GameScene);
                await SceneService.Instance.LoadScene(SceneKeys.GameScene);
            }
            finally
            {
                _isRestarting = false;
            }
        }
    }
}
