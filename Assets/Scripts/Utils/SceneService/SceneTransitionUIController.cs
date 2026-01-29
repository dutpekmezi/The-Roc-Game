using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils.Signal;

namespace Utils.Scene
{
    public class SceneTransitionUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform _transitionImage;

        private Tween _transitionTween;


        private void Start()
        {
            SignalBus.Get<OnSceneTransitionStarted>().Subscribe(OnSceneTransitionStarted);
        }

        private void OnDestroy()
        {
            SignalBus.Get<OnSceneTransitionStarted>().Unsubscribe(OnSceneTransitionStarted);
        }

        private void OnSceneTransitionStarted(SceneConfig config)
        {
            _transitionTween?.Kill();

            _transitionImage.gameObject.SetActive(true);

            _transitionTween = DOTween.Sequence()
                .Append(_transitionImage.transform.DOScale(1f, .6f))
                .Append(_transitionImage.transform.DOScale(0f, .6f))
                .AppendCallback(() => { _transitionImage.gameObject.SetActive(false); });
        }
    }
}