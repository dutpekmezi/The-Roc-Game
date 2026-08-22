using Game.Systems;
using UnityEngine;
using UnityEngine.UI;
using Utils.Popup;
using Utils.Scene;
using VContainer;

namespace Game.UI
{
    public class GameModPopUp : PopupBase
    {
        public const string PopupKey = "game_mod_popup";
        public override string PopupId => PopupKey;

        [SerializeField] private Button flyGameButton;
        [SerializeField] private Button runGameButton;

        private ISceneService sceneService;
        private EnergyService energyService;
        private bool isLoading;

        [Inject]
        private void Construct(ISceneService sceneService, EnergyService energyService)
        {
            this.sceneService = sceneService;
            this.energyService = energyService;
        }

        protected override void Awake()
        {
            base.Awake();
            PostAppear += BindButtons;
        }

        private void Start()
        {
            BindButtons();
        }

        private void OnDestroy()
        {
            PostAppear -= BindButtons;
            UnbindButtons();
        }

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

        private void BindButtons()
        {
            ResolveButtons();

            if (flyGameButton != null)
            {
                flyGameButton.onClick.RemoveListener(StartFlyGame);
                flyGameButton.onClick.AddListener(StartFlyGame);
            }

            if (runGameButton != null)
            {
                runGameButton.onClick.RemoveListener(StartRunGame);
                runGameButton.onClick.AddListener(StartRunGame);
            }
        }

        private void UnbindButtons()
        {
            if (flyGameButton != null)
            {
                flyGameButton.onClick.RemoveListener(StartFlyGame);
            }

            if (runGameButton != null)
            {
                runGameButton.onClick.RemoveListener(StartRunGame);
            }
        }

        private void ResolveButtons()
        {
            flyGameButton ??= FindButton("FlyGameImage");
            runGameButton ??= FindButton("RunGameImage");
        }

        private Button FindButton(string objectName)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == objectName)
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private void StartFlyGame()
        {
            LoadAndStart(SceneKeys.GameScene);
        }

        private void StartRunGame()
        {
            LoadAndStart(SceneKeys.RunGameScene);
        }

        private async void LoadAndStart(string sceneKey)
        {
            if (isLoading)
            {
                return;
            }

            isLoading = true;
            SetButtonsInteractable(false);

            var service = sceneService ?? SceneService.Instance;
            if (service == null)
            {
                Debug.LogWarning("[GameModPopUp] SceneService bulunamadi, oyun modu yuklenemedi.");
                isLoading = false;
                SetButtonsInteractable(true);
                return;
            }

            var energy = energyService ?? EnergyService.Instance;
            if (energy == null)
            {
                Debug.LogWarning("[GameModPopUp] EnergyService bulunamadi, oyun modu baslatilamadi.");
                ShowNoEnergyAndUnlockButtons();
                return;
            }

            var gameState = GameState.Instance;
            if (gameState == null)
            {
                Debug.LogWarning("[GameModPopUp] GameState bulunamadi, oyun modu baslatilamadi.");
                isLoading = false;
                SetButtonsInteractable(true);
                return;
            }

            bool energySpentBeforeSceneLoad = false;
            if (energy.CurrentEnergy <= 0)
            {
                bool canStart = await energy.TrySpendForRunStartAsync();
                if (!canStart)
                {
                    ShowNoEnergyAndUnlockButtons();
                    return;
                }

                energySpentBeforeSceneLoad = true;
                gameState.MarkNextGameStartEnergySpent();
            }

            gameState.RequestImmediateGameStart();
            Disappear();

            try
            {
                await service.LoadScene(sceneKey);
            }
            catch (System.Exception e)
            {
                if (energySpentBeforeSceneLoad)
                {
                    gameState.ConsumeNextGameStartEnergySpent();
                }

                Debug.LogWarning("[GameModPopUp] Oyun modu yuklenemedi: " + e.Message);
            }
        }

        private void ShowNoEnergyAndUnlockButtons()
        {
            isLoading = false;
            SetButtonsInteractable(true);
            NoEnergyPopUp.Show();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            ResolveButtons();

            if (flyGameButton != null)
            {
                flyGameButton.interactable = interactable;
            }

            if (runGameButton != null)
            {
                runGameButton.interactable = interactable;
            }
        }
    }
}
