using DG.Tweening;
using Game.Installers;
using Game.Systems;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Utils.Popup;
using Utils.Scene;

namespace Game.UI
{
    public class NoEnergyPopUp : PopupBase
    {
        public const string PopupKey = "no_energy_popup";
        public override string PopupId => PopupKey;

        [SerializeField] private Button energyButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Transform scaleTarget;
        [SerializeField, Min(0f)] private float appearScaleDuration = 0.2f;
        [SerializeField] private Ease appearScaleEase = Ease.OutBack;

        private Tween appearScaleTween;
        private Vector3 originalScale = Vector3.one;
        private bool originalScaleCached;
        private bool isRoutingToMenu;

        public static bool Show()
        {
            var popupService = PopupService.Instance;
            if (popupService == null)
            {
                return false;
            }

            if (popupService.Get(PopupKey) != null)
            {
                return true;
            }

            return popupService.Create(PopupKey) != null;
        }

        protected override void Awake()
        {
            base.Awake();
            PreAppear += PrepareScaleAnimation;
            PreAppear += PlayScaleAnimation;
            PostAppear += BindButtons;
        }

        private void Start()
        {
            BindButtons();
        }

        private void OnDestroy()
        {
            PreAppear -= PrepareScaleAnimation;
            PreAppear -= PlayScaleAnimation;
            PostAppear -= BindButtons;
            appearScaleTween?.Kill();
            appearScaleTween = null;
        }

        private void PrepareScaleAnimation()
        {
            ResolveScaleTarget();

            if (scaleTarget == null)
            {
                return;
            }

            appearScaleTween?.Kill();
            appearScaleTween = null;

            if (!originalScaleCached)
            {
                originalScale = scaleTarget.localScale;
                originalScaleCached = true;
            }

            scaleTarget.localScale = Vector3.zero;
        }

        private void PlayScaleAnimation()
        {
            ResolveScaleTarget();

            if (scaleTarget == null)
            {
                return;
            }

            appearScaleTween?.Kill();

            if (appearScaleDuration <= 0f)
            {
                scaleTarget.localScale = originalScale;
                appearScaleTween = null;
                return;
            }

            appearScaleTween = scaleTarget
                .DOScale(originalScale, appearScaleDuration)
                .SetEase(appearScaleEase)
                .SetLink(scaleTarget.gameObject);
        }

        private void ResolveScaleTarget()
        {
            if (scaleTarget != null)
            {
                return;
            }

            var panel = transform.Find("Panel");
            scaleTarget = panel != null ? panel : transform;
        }

        private void BindButtons()
        {
            energyButton ??= FindButton("RestartButton");
            menuButton ??= FindButton("SceneChangeButton");

            ConfigureButton(energyButton, OpenEnergyPopup);
            ConfigureButton(menuButton, GoToMenu);
        }

        private Button FindButton(string buttonName)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == buttonName)
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private void ConfigureButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            button.interactable = true;
        }

        private async void OpenEnergyPopup()
        {
            var popupService = PopupService.Instance;

            if (isRoutingToMenu)
            {
                return;
            }

            isRoutingToMenu = true;
            SetButtonsInteractable(false);
            Disappear();
            ClosePopup(popupService, SpinPopUp.PopupKey);
            ClosePopup(popupService, GameOverPopup.PopupKey);
            ClosePopup(popupService, GameModPopUp.PopupKey);

            await ReturnToMenuAsync();

            popupService = PopupService.Instance;
            if (popupService != null && popupService.Get(EnergyPopUp.PopupKey) == null)
            {
                popupService.Create(EnergyPopUp.PopupKey);
            }
        }

        private async void GoToMenu()
        {
            var popupService = PopupService.Instance;

            if (isRoutingToMenu)
            {
                return;
            }

            isRoutingToMenu = true;
            SetButtonsInteractable(false);
            Disappear();
            ClosePopup(popupService, SpinPopUp.PopupKey);
            ClosePopup(popupService, GameOverPopup.PopupKey);
            ClosePopup(popupService, GameModPopUp.PopupKey);

            await ReturnToMenuAsync();
        }

        private static async Task ReturnToMenuAsync()
        {
            if (GameInstaller.Instance != null)
            {
                GameInstaller.Instance.RestartToMenu();
                await Task.Yield();
                return;
            }

            if (RunGameInstaller.Instance != null)
            {
                RunGameInstaller.Instance.RestartToMenu();
                await Task.Yield();
                return;
            }

            GameState.Instance?.SetState(GameFlowState.Menu);

            var sceneService = SceneService.Instance;
            if (sceneService != null)
            {
                await sceneService.LoadScene(SceneKeys.MenuBaseScene);
                return;
            }

            MenuCurrencyRewardFlyer.Instance?.ShowForMenu();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (energyButton != null)
            {
                energyButton.interactable = interactable;
            }

            if (menuButton != null)
            {
                menuButton.interactable = interactable;
            }
        }

        private void ClosePopup(PopupService popupService, string popupId)
        {
            var popup = popupService != null ? popupService.Get(popupId) : null;
            if (popup != null && popup != this)
            {
                popup.Disappear();
            }
        }
    }
}
