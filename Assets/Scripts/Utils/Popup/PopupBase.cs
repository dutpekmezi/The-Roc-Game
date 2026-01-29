using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Utils.Popup
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupBase : MonoBehaviour
    {
        [SerializeField] private List<ComponentBase> extensionBases;
        [SerializeField] private CanvasGroup canvasGroup;

        public Action PostAppear;
        public Action PostDisappear;
        public Action PreAppear;
        public Action PreDisappear;

        public abstract string PopupId { get; }

        protected virtual void Awake()
        {
            PostDisappear += () => Destroy(gameObject);
        }

        public void Disappear()
        {
            canvasGroup.blocksRaycasts = false;
            PreDisappear?.Invoke();

            if (extensionBases.Count == 0)
            {
                PostDisappear?.Invoke();
                return;
            }

            extensionBases.ForEach(x => x.Disappear());
            var maxDuration = extensionBases.Max(x => x.disappearDuration);
            DOVirtual.DelayedCall(maxDuration, () => PostDisappear?.Invoke()).SetLink(gameObject);
        }

        public void Appear()
        {
            canvasGroup.blocksRaycasts = true;
            PreAppear?.Invoke();

            if (extensionBases.Count == 0)
            {
                PostAppear?.Invoke();
                return;
            }

            extensionBases.ForEach(x => x.Appear());
            var maxDuration = extensionBases.Max(x => x.disappearDuration);
            DOVirtual.DelayedCall(maxDuration, () => PostAppear?.Invoke()).SetLink(gameObject);
        }
    }
}