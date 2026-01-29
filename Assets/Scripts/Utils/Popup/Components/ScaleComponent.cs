using DG.Tweening;
namespace Utils.Popup
{
    public class ScaleComponent : ComponentBase
    {
        protected override void InstantDisappear()
        {
            transform.DOScale(0, 0);
        }

        public override void Disappear()
        {
            transform.DOScale(0, disappearDuration)
                .SetLink(transform.gameObject);
        }

        public override void Appear()
        {
            transform.DOScale(targetValue, appearDuration)
                .SetEase(ease)
                .SetLink(transform.gameObject);
        }
    }
}