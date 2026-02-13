using Game.Systems;
using UnityEngine;
using Utils.Currency;
using Utils.Level;
using Utils.Save;
using Utils.Scene;
using Utils.Signal;

namespace Game.Installers
{
    public class CoreInstaller : MonoBehaviour
    {
        [SerializeField] private SceneServiceSettings sceneServiceSettings;
        [SerializeField] private CurrencyServiceSettings currencyServiceSettings;
        [SerializeField] private LevelSettings levelSettings;
        [SerializeField] private bool persistBetweenScenes = true;
        [SerializeField] private Canvas canvas;

        [SerializeField] public Canvas Canvas =>  canvas;

        private bool _initialized;

        public static CoreInstaller Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
            }

            Instance = this;

            if (_initialized)
            {
                return;
            }

            _initialized = true;

            if (persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            SignalBus.Clear();
            InitializeSaveService();
            InitializeSceneService();
            InitializeCurrencyService();
            InitializeGameState();
        }

        private static void InitializeSaveService()
        {
            if (SaveService.Instance != null)
            {
                return;
            }

            _ = new SaveService(new EncryptedSaveHandler());
        }

        private void InitializeSceneService()
        {
            if (SceneService.Instance != null)
            {
                return;
            }

            if (sceneServiceSettings == null)
            {
                Debug.LogWarning("[CoreInstaller] SceneServiceSettings is not assigned.");
                return;
            }

            _ = new SceneService(sceneServiceSettings);

            _ = SceneService.Instance.LoadScene(SceneKeys.MenuScene);
        }

        private void InitializeCurrencyService()
        {
            if (CurrencyService.Instance != null)
            {
                return;
            }

            if (currencyServiceSettings == null)
            {
                Debug.LogWarning("[CoreInstaller] CurrencyServiceSettings is not assigned.");
                return;
            }

            _ = new CurrencyService(currencyServiceSettings);
        }

        private void InitializeGameState()
        {
            if (GameState.Instance != null)
            {
                return;
            }

            _ = new GameState();
        }

        /*private void InitializeLevelService()
        {
            if (LevelService.Instance != null)
            {
                return;
            }

            if (levelSettings == null)
            {
                Debug.LogWarning("[CoreInstaller] LevelSettings is not assigned.");
                return;
            }

            _ = new LevelService(levelSettings);
        }*/
    }
}
