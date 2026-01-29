using DG.Tweening;
using UnityEngine;

namespace Utils.Popup
{
    public abstract class ComponentBase : MonoBehaviour
    {
        [SerializeField] public float appearDuration;
        [SerializeField] public float disappearDuration;
        [SerializeField] protected float targetValue;
        [SerializeField] protected Ease ease = Ease.OutQuad;

        private void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            InstantDisappear();
        }

        protected abstract void InstantDisappear();
        public abstract void Disappear();
        public abstract void Appear();
    }
}