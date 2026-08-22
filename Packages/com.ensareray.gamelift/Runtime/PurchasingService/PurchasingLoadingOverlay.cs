using GameLift.Popup;

namespace GameLift.Purchasing
{
    public class PurchasingLoadingOverlay : PopupBase
    {
        public override string PopupId => PopupIdConst;

        public const string PopupIdConst = "purchasing_loading";
    }
}
