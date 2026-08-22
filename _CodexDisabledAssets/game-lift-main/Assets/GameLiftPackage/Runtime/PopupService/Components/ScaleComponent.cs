using DG.Tweening;
namespace GameLift.Popup
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
                .SetDelay(delay)
                .SetLink(transform.gameObject);
        }

        public override void Appear()
        {
            transform.DOScale(targetValue, appearDuration)
                .SetDelay(delay)
                .SetEase(ease)
                .SetLink(transform.gameObject);
        }
    }
}