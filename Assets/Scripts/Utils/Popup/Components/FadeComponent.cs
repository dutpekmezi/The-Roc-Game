using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
namespace Utils.Popup
{
    [RequireComponent(typeof(Graphic))]
    public class FadeComponent : ComponentBase
    {
        [SerializeField] private Graphic source;

        protected override void Initialize()
        {
            base.Initialize();
            source = GetComponent<Graphic>();
        }

        protected override void InstantDisappear()
        {
            source.DOFade(0, 0);
        }

        public override void Disappear()
        {
            source.DOFade(0, disappearDuration)
                .SetLink(source.gameObject);
        }

        public override void Appear()
        {
            source.DOFade(targetValue, appearDuration)
                .SetEase(ease)
                .SetLink(source.gameObject);
        }
    }
}