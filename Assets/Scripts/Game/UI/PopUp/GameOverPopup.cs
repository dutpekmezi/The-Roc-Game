using Game.Systems;
using System.Collections.Generic;
using UnityEngine;
using Utils.Popup;

namespace Game.UI
{
    public class GameOverPopup : PopupBase
    {
        public const string PopupKey = "game_over";
        [SerializeField] private List<CollectableBar> collectableBars = new();

        public override string PopupId => PopupKey;

        protected override void Awake()
        {
            base.Awake();
            CacheCollectableBars();
            PostAppear += FlyCollectedCollectablesToBars;
        }

        private void CacheCollectableBars()
        {
            if (collectableBars == null || collectableBars.Count == 0)
            {
                collectableBars = new List<CollectableBar>(GetComponentsInChildren<CollectableBar>(true));
            }
        }

        private void FlyCollectedCollectablesToBars()
        {
            if (CollectableSystem.Instance == null)
            {
                return;
            }

            CacheCollectableBars();

            foreach (var collectableBar in collectableBars)
            {
                if (collectableBar == null)
                {
                    continue;
                }

                var config = collectableBar.CollectableConfig;
                if (config == null)
                {
                    continue;
                }

                if (!CollectableSystem.Instance.TryGetCollectedCount(config, out var count))
                {
                    continue;
                }

                if (collectableBar.IconRectTransform == null)
                {
                    continue;
                }

                var endScreenPos = RectTransformUtility.WorldToScreenPoint(null, collectableBar.IconRectTransform.position);
                CollectableSystem.Instance.FlyCollectedCollectablesToScreenPosition(config, endScreenPos, count);
                collectableBar.SetCount(count);
            }
        }
    }
}
