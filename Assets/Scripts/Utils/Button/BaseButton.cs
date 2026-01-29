using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Utils.Buttons
{
    [RequireComponent(typeof(Button))]
    public class BaseButton : MonoBehaviour
    {
        [Header("Base Button")]
        //[SerializeField] protected SoundFileObject clickSound;
        [SerializeField] protected string buttonId;
        [SerializeField] private Button button;

        [Header("Base Animations")]
        [SerializeField] private bool animate = false;
        [SerializeField] private float scale;
        [SerializeField] private float duration;
        [SerializeField] private Ease ease;


        //private TapTutorialHand tapTutorialHand;

        public string AnimKey => "base_button_" + this.GetHashCode();
        public Button Button => button;
        public string ButtonId => buttonId;
        //public TapTutorialHand TapTutorialHand => tapTutorialHand;

        private void Awake()
        {
            button = GetComponent<Button>();

            button.onClick.AddListener(BaseOnClick);
        }

        protected virtual void OnEnable()
        {
            ButtonManager.RegisterButton(this);

            if (animate)
            {
                DOTween.Kill(AnimKey);

                transform.DOScale(scale, duration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(ease)
                    .SetId(AnimKey)
                    .SetLink(gameObject);
            }
        }

        protected virtual void OnDisable()
        {
            ButtonManager.UnregisterButton(this);

            //HideTutorialHand();
        }

        public virtual void BaseOnClick()
        {
            ButtonManager.Instance.OnButtonClickedHandler(this);
            DOTween.Kill(AnimKey);

            transform.DOScale(Vector3.one * 0.85f, 0.15f)
                .From(Vector3.one)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (transform != null)
                    {
                        transform.DOScale(Vector3.one, 0.15f)
                            .SetEase(Ease.OutQuad)
                            .SetLink(gameObject);
                    }
                })
                .SetLink(gameObject);

            //ResolveServices.AudioService.PlaySound(clickSound);
        }

        /*public async void ShowTutorialHand()
        {
            if (tapTutorialHand == null)
            {
                tapTutorialHand = ResolveServices.PoolService.Spawn(ButtonManager.Instance.TapTutorialHand);

                int i = 0;
                while (tapTutorialHand == null && i < 5)
                {
                    tapTutorialHand = ResolveServices.PoolService.Spawn(ButtonManager.Instance.TapTutorialHand);

                    await UniTask.Yield();

                    ++i;
                }

                if (tapTutorialHand == null)
                    return;

                tapTutorialHand.transform.SetParent(transform, false);
                tapTutorialHand.RectTransform.anchoredPosition = Vector2.zero;
                tapTutorialHand.DoAnimation();
            }
        }

        public async void HideTutorialHand()
        {
            if (tapTutorialHand != null)
            {
                await UniTask.Yield();

                if (tapTutorialHand != null)
                {
                    tapTutorialHand.Close();
                    ResolveServices.PoolService.Despawn(tapTutorialHand.gameObject);

                    tapTutorialHand = null;
                }
            }
        }*/
    }
}