using System.Collections;
using System.Collections.Generic;
using Game.Installers;
using Game.Systems;
using UnityEngine;
using UnityEngine.UI;
using Utils.Currency;
using Utils.ObjectFlowAnimator;
using Utils.Scene;

namespace Game.UI
{
    public class MenuCurrencyRewardFlyer : MonoBehaviour
    {
        [SerializeField] private RectTransform canvas;
        [SerializeField] private Camera cam;
        [SerializeField] private SceneChangeButton playButton;
        [SerializeField] private List<CurrencyBar> currencyBarList = new List<CurrencyBar>();

        public List<CurrencyBar> CurrencyBarList => currencyBarList;

        public static MenuCurrencyRewardFlyer Instance {  get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance);
            }

            Instance = this;

            GameState.Instance.SetState(GameFlowState.Menu);
        }

        private void Start()
        {
            FlyRewards();
        }

        public bool FlyGold(Vector2 startScreenPos, int amount = 1)
        {
            return FlyCurrency(CollectableIds.Coin, startScreenPos, amount);
        }

        public bool FlyCurrency(string currencyId, Vector2 startScreenPos, int amount = 1)
        {
            if (string.IsNullOrEmpty(currencyId) || amount <= 0)
            {
                return false;
            }

            EnsureLayoutIsUpToDate();

            var currencyBar = GetCurrencyBar(currencyId);
            if (currencyBar == null)
            {
                return false;
            }

            var currencyConfig = currencyBar.CurrencyConfig;
            if (currencyConfig == null || currencyBar.IconRectTransform == null)
            {
                return false;
            }

            if (UIFlowAnimator.Instance == null)
            {
                return false;
            }

            if (CurrencyService.Instance != null)
            {
                CurrencyService.Instance.AddFakeCurrency(currencyConfig.currencyId, -amount);
            }

            UIFlowAnimator.Instance.AddNewDestinationAction(
                startScreenPos: startScreenPos,
                endScreenPosProvider: () => GetScreenPoint(currencyBar.IconRectTransform),
                sprite: currencyConfig.currencySprite,
                parent: CoreInstaller.Instance.Canvas.transform as RectTransform,
                particleCount: amount,
                destinationActionData: currencyConfig.destinationActionData,
                prefab: currencyConfig.currencyUIPrefab,
                onReceivedItem: () =>
                {
                    CurrencyService.Instance?.AddFakeCurrency(currencyConfig.currencyId, 1);
                }
            );

            return true;
        }

        private void FlyRewards()
        {
            if (GameState.Instance == null)
            {
                return;
            }

            Dictionary<string, int> rewards = GameState.Instance.ConsumePendingCurrencyRewards();
            if (rewards.Count == 0)
            {
                return;
            }

            if (UIFlowAnimator.Instance == null)
            {
                return;
            }

            if (playButton == null)
            {
                return;
            }

            EnsureLayoutIsUpToDate();

            var startScreenPos = GetScreenPoint(playButton.Transform);

            foreach (var currencyBar in currencyBarList)
            {
                if (currencyBar == null)
                {
                    continue;
                }

                var currencyConfig = currencyBar.CurrencyConfig;
                if (currencyConfig == null)
                {
                    continue;
                }

                if (!rewards.TryGetValue(currencyConfig.currencyId, out var amount) || amount <= 0)
                {
                    continue;
                }

                if (currencyBar.IconRectTransform == null)
                {
                    continue;
                }

                FlyCurrency(currencyConfig.currencyId, startScreenPos, amount);
            }
        }

        private CurrencyBar GetCurrencyBar(string currencyId)
        {
            if (string.IsNullOrEmpty(currencyId) || currencyBarList == null)
            {
                return null;
            }

            for (int i = 0; i < currencyBarList.Count; i++)
            {
                var currencyBar = currencyBarList[i];
                if (currencyBar == null || currencyBar.CurrencyConfig == null)
                {
                    continue;
                }

                if (currencyBar.CurrencyConfig.currencyId == currencyId)
                {
                    return currencyBar;
                }
            }

            return null;
        }

        private void EnsureLayoutIsUpToDate()
        {
            Canvas.ForceUpdateCanvases();

            if (canvas != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvas);
            }

            for (int i = 0; i < currencyBarList.Count; i++)
            {
                var currencyBar = currencyBarList[i];
                if (currencyBar?.ParentRectTransform != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(currencyBar.ParentRectTransform);
                }
            }
        }

        private Vector2 GetScreenPoint(RectTransform targetRect)
        {
            if (targetRect == null)
            {
                return Vector2.zero;
            }

            var targetCanvas = targetRect.GetComponentInParent<Canvas>();
            Camera targetCamera = null;

            if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                targetCamera = targetCanvas.worldCamera != null ? targetCanvas.worldCamera : cam;
            }

            return RectTransformUtility.WorldToScreenPoint(targetCamera, targetRect.position);
        }

        /*private Vector2 GetScreenPoint(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return Vector2.zero;
            }

            Camera camera = Camera.main;

            return RectTransformUtility.WorldToScreenPoint(camera, rectTransform.position);
        }*/
    }
}
