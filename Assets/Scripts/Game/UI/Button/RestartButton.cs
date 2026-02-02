using Game.Installers;
using Utils.Buttons;
public class RestartButton : BaseButton
{
    public override void BaseOnClick()
    {
        base.BaseOnClick();

        if (GameInstaller.Instance != null)
        {
            GameInstaller.Instance.RestartGame();
        }
    }
}
