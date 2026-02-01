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

        private bool _initialized;

        private void Awake()
        {
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

            _ = SceneService.Instance.LoadScene(SceneKeys.GameScene);
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
