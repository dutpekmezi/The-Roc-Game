using Game.Installers;
using Utils.Buttons;
using Utils.Scene;

namespace Game.UI
{
    public class RestartButton : BaseButton
    {
        public override void BaseOnClick()
        {
            base.BaseOnClick();

            GameInstaller.Instance.Clear();

            _ = SceneService.Instance.RemoveScene(SceneKeys.GameScene);
            _ = SceneService.Instance.LoadScene(SceneKeys.GameScene);
        }
    }
}