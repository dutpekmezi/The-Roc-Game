using DG.Tweening;
using UnityEngine;

namespace Utils.Popup
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
            source.DOFade(0, disappearDuration)
                .SetLink(source.gameObject);
        }

        public override void Appear()
        {
            source.DOFade(targetValue, appearDuration)
                .From(0)
                .SetEase(ease)
                .SetLink(source.gameObject);
        }
    }
}