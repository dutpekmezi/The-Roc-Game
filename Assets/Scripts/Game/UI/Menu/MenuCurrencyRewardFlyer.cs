using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Installers;
using Game.Systems;
using UnityEngine;
using UnityEngine.UI;
using Utils.Currency;
using Utils.ObjectFlowAnimator;
using Utils.Scene;
using VContainer;

namespace Game.UI
{
    public class MenuCurrencyRewardFlyer : MonoBehaviour
    {
        private const string CoinArrivalSoundName = "Clink";

        [SerializeField] private RectTransform canvas;
        [SerializeField] private Canvas menuCanvas;
        [SerializeField] private CanvasGroup menuCanvasGroup;
        [SerializeField] private Camera cam;
        [SerializeField] private RectTransform CollectableFlyStartPoint;
        [SerializeField] private List<CurrencyBar> currencyBarList = new List<CurrencyBar>();

        [Header("HUD Transition")]
        [SerializeField] private RectTransform energyBar;
        [SerializeField] private RectTransform currencyBarParent;
        [SerializeField] private RectTransform buttonsParent;
        [SerializeField] private RectTransform tapImage;
        [SerializeField] private float hudMoveDuration = 0.35f;
        [SerializeField] private Ease hudMoveEase = Ease.InOutQuad;
        [SerializeField] private float hudOutsidePadding = 32f;

        public List<CurrencyBar> CurrencyBarList => currencyBarList;

        public static MenuCurrencyRewardFlyer Instance {  get; private set; }
        private ICurrencyService _currencyService;
        private IUIFlowAnimator _uiFlowAnimator;
        private readonly Dictionary<RectTransform, Vector2> hudVisiblePositions = new Dictionary<RectTransform, Vector2>();
        private GameState subscribedGameState;
        private bool hudIsHidden;
        private bool hudHideInProgress;
        private bool hudShowInProgress;
        private int pendingHudHideTweens;
        private int pendingHudShowTweens;
        private bool tapImageForceHidden;
        private bool flyPendingRewardsWhenHudShown;
        private readonly Vector3[] hudTargetCorners = new Vector3[4];
        private readonly Vector3[] hudCanvasCorners = new Vector3[4];

        public bool IsHudTransitionInProgress => hudHideInProgress || hudShowInProgress;

        [Inject]
        private void Construct(
            ICurrencyService currencyService,
            IUIFlowAnimator uiFlowAnimator)
        {
            _currencyService = currencyService;
            _uiFlowAnimator = uiFlowAnimator;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance);
            }

            Instance = this;
            ResolveMenuCanvasReferences();
            CacheHudVisiblePositions(true);
            BindGameState();

            var currentState = GameState.Instance?.CurrentState ?? GameFlowState.Menu;
            if (!IsTapHiddenState(currentState))
            {
                GameState.Instance?.SetState(GameFlowState.Menu);
                currentState = GameFlowState.Menu;
            }

            ApplyHudState(currentState, true);
        }

        private void Start()
        {
            BindGameState();
            QueuePendingRewardFly();
        }

        private void OnDisable()
        {
            UnbindGameState();
            KillHudTweens();
            hudHideInProgress = false;
            hudShowInProgress = false;
            pendingHudHideTweens = 0;
            pendingHudShowTweens = 0;
        }

        private void LateUpdate()
        {
            if (tapImageForceHidden || IsTapHiddenState(GameState.Instance?.CurrentState ?? GameFlowState.Menu))
            {
                SetTapImageVisible(false);

                if (hudIsHidden && !hudHideInProgress && IsMenuCanvasVisible())
                {
                    SetMenuCanvasVisible(false);
                }
            }
        }

        public static void HideTapImagesForGameplay()
        {
            var instance = Instance != null
                ? Instance
                : FindFirstObjectByType<MenuCurrencyRewardFlyer>(FindObjectsInactive.Include);

            if (instance != null)
            {
                instance.HideForGameplay();
                return;
            }

            SetNamedTapImagesVisible(false);
            SetNamedMenuCanvasesVisible(false);
        }

        public void HideForGameplay()
        {
            tapImageForceHidden = true;
            HideHud(false);
        }

        public void ShowForMenu()
        {
            QueuePendingRewardFly();
            ShowHud(false);
            TryFlyQueuedRewardsIfReady();
        }

        public bool FlyGold(Vector2 startScreenPos, int amount = 1, bool rewardAlreadyApplied = true)
        {
            return FlyCurrency(CollectableIds.Coin, startScreenPos, amount, rewardAlreadyApplied);
        }

        public bool FlyCurrency(
            string currencyId,
            Vector2 startScreenPos,
            int amount = 1,
            bool rewardAlreadyApplied = true)
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

            var uiFlowAnimator = _uiFlowAnimator ?? UIFlowAnimator.Instance;
            var currencyService = _currencyService ?? CurrencyService.Instance;

            if (uiFlowAnimator == null || currencyService == null)
            {
                return false;
            }

            if (rewardAlreadyApplied)
            {
                currencyService.AddFakeCurrency(currencyConfig.currencyId, -amount);
            }

            var flowParent = CoreInstaller.Instance?.Canvas != null
                ? CoreInstaller.Instance.Canvas.transform as RectTransform
                : canvas;

            uiFlowAnimator.AddNewDestinationAction(
                startScreenPos: startScreenPos,
                endScreenPosProvider: () => GetScreenPoint(currencyBar.IconRectTransform),
                sprite: currencyConfig.currencySprite,
                parent: flowParent,
                particleCount: amount,
                destinationActionData: currencyConfig.destinationActionData,
                prefab: currencyConfig.currencyUIPrefab,
                onReceivedItem: () =>
                {
                    if (rewardAlreadyApplied)
                    {
                        currencyService.AddFakeCurrency(currencyConfig.currencyId, 1);
                    }
                    else
                    {
                        currencyService.ModifyCurrency(currencyConfig.currencyId, 1);
                    }
                },
                receivedSoundName: currencyConfig.currencyId == CollectableIds.Coin
                    ? CoinArrivalSoundName
                    : null
            );

            return true;
        }

        private void QueuePendingRewardFly()
        {
            if (GameState.Instance == null || !GameState.Instance.HasPendingCurrencyRewards)
            {
                return;
            }

            flyPendingRewardsWhenHudShown = true;
            TryFlyQueuedRewardsIfReady();
        }

        private void TryFlyQueuedRewardsIfReady()
        {
            if (!flyPendingRewardsWhenHudShown)
            {
                return;
            }

            var gameState = GameState.Instance;
            if (gameState == null || !gameState.HasPendingCurrencyRewards)
            {
                flyPendingRewardsWhenHudShown = false;
                return;
            }

            if (hudIsHidden || IsHudTransitionInProgress || tapImageForceHidden || IsTapHiddenState(gameState.CurrentState))
            {
                return;
            }

            if (FlyRewards())
            {
                flyPendingRewardsWhenHudShown = false;
            }
        }

        private bool FlyRewards()
        {
            var gameState = GameState.Instance;
            if (gameState == null)
            {
                return false;
            }

            Dictionary<string, int> rewards = gameState.GetPendingCurrencyRewardsSnapshot();
            if (rewards.Count == 0)
            {
                return true;
            }

            var uiFlowAnimator = _uiFlowAnimator ?? UIFlowAnimator.Instance;
            if (uiFlowAnimator == null)
            {
                return false;
            }

            if (CollectableFlyStartPoint == null)
            {
                return false;
            }

            EnsureLayoutIsUpToDate();

            var startScreenPos = GetScreenPoint(CollectableFlyStartPoint);
            var flownRewards = new Dictionary<string, int>();
            var clearedExistingFlowActions = false;

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

                if (flownRewards.ContainsKey(currencyConfig.currencyId))
                {
                    continue;
                }

                if (currencyBar.IconRectTransform == null)
                {
                    continue;
                }

                if (!clearedExistingFlowActions)
                {
                    uiFlowAnimator.CancelAllDestinationActions();
                    clearedExistingFlowActions = true;
                }

                if (FlyCurrency(currencyConfig.currencyId, startScreenPos, amount))
                {
                    flownRewards[currencyConfig.currencyId] = amount;
                }
            }

            if (flownRewards.Count == 0)
            {
                return false;
            }

            gameState.RemovePendingCurrencyRewards(flownRewards);
            return true;
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

        private void BindGameState()
        {
            if (GameState.Instance == null || subscribedGameState == GameState.Instance)
            {
                return;
            }

            UnbindGameState();
            subscribedGameState = GameState.Instance;
            subscribedGameState.StateChanged += HandleGameStateChanged;
            ApplyHudState(subscribedGameState.CurrentState, true);
        }

        private void UnbindGameState()
        {
            if (subscribedGameState == null)
            {
                return;
            }

            subscribedGameState.StateChanged -= HandleGameStateChanged;
            subscribedGameState = null;
        }

        private void HandleGameStateChanged(GameFlowState state)
        {
            ApplyHudState(state, false);
        }

        private void ApplyHudState(GameFlowState state, bool immediate)
        {
            if (state == GameFlowState.InGame || state == GameFlowState.GameOver)
            {
                HideHud(immediate);
                return;
            }

            if (state == GameFlowState.Menu || state == GameFlowState.WaitingToStart)
            {
                ShowHud(immediate);
            }
        }

        private void HideHud(bool immediate)
        {
            SetTapImageVisible(false);
            SetMenuCanvasInputEnabled(false);

            if (hudIsHidden || hudHideInProgress)
            {
                if (hudIsHidden && !hudHideInProgress && IsMenuCanvasVisible())
                {
                    SetMenuCanvasVisible(false);
                }

                return;
            }

            CacheHudVisiblePositions(false);
            hudShowInProgress = false;
            pendingHudShowTweens = 0;
            hudHideInProgress = true;
            pendingHudHideTweens = 0;

            TrackHudHideTween(MoveHudTarget(energyBar, false, immediate));
            TrackHudHideTween(MoveHudTarget(currencyBarParent, true, immediate));
            TrackHudHideTween(MoveHudTarget(buttonsParent, true, immediate));
            hudIsHidden = true;

            if (pendingHudHideTweens == 0)
            {
                CompleteHudHide();
            }
        }

        private void ShowHud(bool immediate)
        {
            tapImageForceHidden = false;
            hudHideInProgress = false;
            pendingHudHideTweens = 0;
            hudShowInProgress = false;
            pendingHudShowTweens = 0;
            SetMenuCanvasVisible(true);
            SetMenuCanvasInputEnabled(false);

            TrackHudShowTween(MoveHudTargetToVisible(energyBar, immediate));
            TrackHudShowTween(MoveHudTargetToVisible(currencyBarParent, immediate));
            TrackHudShowTween(MoveHudTargetToVisible(buttonsParent, immediate));
            SetTapImageVisible(true);
            hudIsHidden = false;

            if (pendingHudShowTweens == 0)
            {
                CompleteHudShow();
            }
            else
            {
                hudShowInProgress = true;
            }
        }

        private Tween MoveHudTarget(RectTransform target, bool moveUp, bool immediate)
        {
            if (target == null)
            {
                return null;
            }

            if (!hudVisiblePositions.TryGetValue(target, out var visiblePosition))
            {
                visiblePosition = target.anchoredPosition;
                hudVisiblePositions[target] = visiblePosition;
            }

            return MoveHudTargetTo(target, GetHiddenPosition(target, visiblePosition, moveUp), immediate);
        }

        private Tween MoveHudTargetToVisible(RectTransform target, bool immediate)
        {
            if (target == null)
            {
                return null;
            }

            if (!hudVisiblePositions.TryGetValue(target, out var visiblePosition))
            {
                visiblePosition = target.anchoredPosition;
                hudVisiblePositions[target] = visiblePosition;
            }

            return MoveHudTargetTo(target, visiblePosition, immediate);
        }

        private Tween MoveHudTargetTo(RectTransform target, Vector2 targetPosition, bool immediate)
        {
            DOTween.Kill(GetHudTweenId(target));

            if (immediate || hudMoveDuration <= 0f || Vector2.SqrMagnitude(target.anchoredPosition - targetPosition) <= 0.01f)
            {
                target.anchoredPosition = targetPosition;
                return null;
            }

            return target.DOAnchorPos(targetPosition, hudMoveDuration)
                .SetEase(hudMoveEase)
                .SetId(GetHudTweenId(target))
                .SetLink(target.gameObject);
        }

        private void TrackHudHideTween(Tween tween)
        {
            if (tween == null)
            {
                return;
            }

            pendingHudHideTweens++;
            tween.OnComplete(HandleHudHideTweenComplete);
        }

        private void TrackHudShowTween(Tween tween)
        {
            if (tween == null)
            {
                return;
            }

            pendingHudShowTweens++;
            tween.OnComplete(HandleHudShowTweenComplete);
        }

        private void HandleHudHideTweenComplete()
        {
            pendingHudHideTweens = Mathf.Max(0, pendingHudHideTweens - 1);
            if (pendingHudHideTweens == 0)
            {
                CompleteHudHide();
            }
        }

        private void HandleHudShowTweenComplete()
        {
            pendingHudShowTweens = Mathf.Max(0, pendingHudShowTweens - 1);
            if (pendingHudShowTweens == 0)
            {
                CompleteHudShow();
            }
        }

        private void CompleteHudHide()
        {
            hudHideInProgress = false;
            pendingHudHideTweens = 0;

            if (tapImageForceHidden || IsTapHiddenState(GameState.Instance?.CurrentState ?? GameFlowState.Menu))
            {
                SetMenuCanvasVisible(false);
            }
        }

        private void CompleteHudShow()
        {
            hudShowInProgress = false;
            pendingHudShowTweens = 0;

            if (tapImageForceHidden || IsTapHiddenState(GameState.Instance?.CurrentState ?? GameFlowState.Menu))
            {
                SetMenuCanvasInputEnabled(false);
                return;
            }

            CacheHudVisiblePositions(true);
            SetMenuCanvasInputEnabled(true);
            TryFlyQueuedRewardsIfReady();
        }

        private Vector2 GetHiddenPosition(RectTransform target, Vector2 visiblePosition, bool moveUp)
        {
            Canvas.ForceUpdateCanvases();

            var canvasRect = ResolveCanvasRect(target);
            var parent = target.parent as RectTransform;
            if (canvasRect == null || parent == null)
            {
                var fallbackDistance = Mathf.Max(target.rect.height, target.sizeDelta.y) + hudOutsidePadding;
                return visiblePosition + (moveUp ? Vector2.up : Vector2.down) * fallbackDistance;
            }

            target.GetWorldCorners(hudTargetCorners);
            canvasRect.GetWorldCorners(hudCanvasCorners);

            var worldDeltaY = moveUp
                ? hudCanvasCorners[1].y - hudTargetCorners[0].y + hudOutsidePadding
                : hudCanvasCorners[0].y - hudTargetCorners[1].y - hudOutsidePadding;

            var localStart = (Vector2)parent.InverseTransformPoint(target.position);
            var localEnd = (Vector2)parent.InverseTransformPoint(target.position + new Vector3(0f, worldDeltaY, 0f));

            return visiblePosition + (localEnd - localStart);
        }

        private RectTransform ResolveCanvasRect(RectTransform target)
        {
            if (canvas != null)
            {
                return canvas;
            }

            var targetCanvas = target.GetComponentInParent<Canvas>();
            if (targetCanvas != null)
            {
                return targetCanvas.transform as RectTransform;
            }

            return transform as RectTransform;
        }

        private void ResolveMenuCanvasReferences()
        {
            if (canvas == null)
            {
                canvas = transform as RectTransform;
            }

            if (menuCanvas == null)
            {
                menuCanvas = canvas != null ? canvas.GetComponent<Canvas>() : null;
            }

            if (menuCanvas == null)
            {
                menuCanvas = GetComponent<Canvas>();
            }

            var canvasGameObject = menuCanvas != null
                ? menuCanvas.gameObject
                : canvas != null
                    ? canvas.gameObject
                    : gameObject;

            if (menuCanvasGroup == null)
            {
                menuCanvasGroup = canvasGameObject.GetComponent<CanvasGroup>();
            }

            if (menuCanvasGroup == null)
            {
                menuCanvasGroup = canvasGameObject.AddComponent<CanvasGroup>();
            }
        }

        private RectTransform FindChildRectTransform(string objectName)
        {
            var rectTransforms = GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                if (rectTransforms[i] != null && rectTransforms[i].gameObject.name == objectName)
                {
                    return rectTransforms[i];
                }
            }

            return null;
        }

        private Image FindTapImage()
        {
            var tapRect = FindChildRectTransform("TapImage");
            return tapRect != null ? tapRect.GetComponent<Image>() : null;
        }

        private void CacheHudVisiblePositions(bool force)
        {
            CacheHudVisiblePosition(energyBar, force);
            CacheHudVisiblePosition(currencyBarParent, force);
            CacheHudVisiblePosition(buttonsParent, force);
        }

        private void CacheHudVisiblePosition(RectTransform target, bool force)
        {
            if (target == null)
            {
                return;
            }

            if (!force && (hudIsHidden || hudHideInProgress || hudShowInProgress))
            {
                return;
            }

            if (!force && DOTween.IsTweening(GetHudTweenId(target)))
            {
                return;
            }

            hudVisiblePositions[target] = target.anchoredPosition;
        }

        private void KillHudTweens()
        {
            KillHudTween(energyBar);
            KillHudTween(currencyBarParent);
            KillHudTween(buttonsParent);
        }

        private void SetTapImageVisible(bool visible)
        {
            if (tapImage != null)
            {
                tapImage.gameObject.SetActive(visible);
            }
        }

        private void SetMenuCanvasVisible(bool visible)
        {
            ResolveMenuCanvasReferences();

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = visible ? 1f : 0f;
                menuCanvasGroup.interactable = visible;
                menuCanvasGroup.blocksRaycasts = visible;
            }

            if (menuCanvas != null)
            {
                menuCanvas.enabled = true;
            }

            Canvas.ForceUpdateCanvases();
        }

        private void SetMenuCanvasInputEnabled(bool enabled)
        {
            ResolveMenuCanvasReferences();

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.interactable = enabled;
                menuCanvasGroup.blocksRaycasts = enabled;
            }
        }

        private bool IsMenuCanvasVisible()
        {
            ResolveMenuCanvasReferences();
            if (menuCanvasGroup != null)
            {
                return menuCanvasGroup.alpha > 0f;
            }

            return menuCanvas != null && menuCanvas.enabled;
        }

        private static bool IsTapHiddenState(GameFlowState state)
        {
            return state == GameFlowState.InGame || state == GameFlowState.GameOver;
        }

        private static void SetNamedTapImagesVisible(bool visible)
        {
            var images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject.name == "TapImage")
                {
                    images[i].enabled = visible;
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        private static void SetNamedMenuCanvasesVisible(bool visible)
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] == null || canvases[i].gameObject.name != "MenuCanvas")
                {
                    continue;
                }

                canvases[i].enabled = true;

                var canvasGroup = canvases[i].GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = canvases[i].gameObject.AddComponent<CanvasGroup>();
                }

                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            Canvas.ForceUpdateCanvases();
        }

        private void KillHudTween(RectTransform target)
        {
            if (target != null)
            {
                DOTween.Kill(GetHudTweenId(target));
            }
        }

        private string GetHudTweenId(RectTransform target)
        {
            return "menu_hud_anchor_" + target.GetInstanceID();
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
