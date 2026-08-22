using DG.Tweening;
using UnityEngine;

namespace GameLift.Popup
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasFadeComponent : ComponentBase
    {
        [SerializeField] private CanvasGroup source;

        protected override void Initialize()
        {
            base.Initialize();
            source = GetComponent<CanvasGroup>();
        }

        protected override void InstantDisappear()
        {
            source.alpha = 0;
        }

        public override void Disappear()
        {
            DOVirtual.Float(1, 0, disappearDuration, (val) =>
                {
                    source.alpha = val;
                }).SetDelay(delay)
                .SetLink(source.gameObject);
        }

        public override void Appear()
        {
            DOVirtual.Float(0, 1, appearDuration, (val) =>
                {
                    source.alpha = val;
                }).SetDelay(delay)
                .SetLink(source.gameObject);
        }
    }
}