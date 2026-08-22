using GameLift.Signal;
using DG.Tweening;
using UnityEngine;
using VContainer;

namespace GameLift.Scene
{
    public class SceneTransitionUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform _transitionImage;

        private Tween _transitionTween;
        private ISignalBus _signalBus;

        [Inject]
        public void Construct(ISignalBus signalBus)
        {
            _signalBus = signalBus;
            _signalBus.Get<OnSceneTransitionStarted>().Subscribe(OnSceneTransitionStarted);
        }

        private void OnDestroy()
        {
            if (_signalBus != null)
                _signalBus.Get<OnSceneTransitionStarted>().Unsubscribe(OnSceneTransitionStarted);
        }

        private void OnSceneTransitionStarted(SceneConfig config)
        {
            _transitionTween?.Kill();

            _transitionImage.gameObject.SetActive(true);

            _transitionTween = DOTween.Sequence()
                .Append(_transitionImage.transform.DOScale(1f, .3f))
                .Append(_transitionImage.transform.DOScale(0f, .3f))
                .AppendCallback(() => { _transitionImage.gameObject.SetActive(false); });
        }
    }
}