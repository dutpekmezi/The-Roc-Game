using DG.Tweening;
using UnityEngine;

namespace GameLift.Popup
{
    public abstract class ComponentBase : MonoBehaviour
    {
        [SerializeField] public float appearDuration;
        [SerializeField] public float disappearDuration;
        [SerializeField] protected float targetValue;
        [SerializeField] protected float delay;
        [SerializeField] protected Ease ease = Ease.OutQuad;

        public float Delay => delay;

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