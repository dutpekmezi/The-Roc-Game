using Game.Systems;
using UnityEngine;
using Utils.Scene;

namespace Game.Installers
{
    public class MenuBaseInstaller : MonoBehaviour
    {
        [SerializeField] private StoreManager storeManager;
        [SerializeField] private SpinRewardSystem spinManager;

        private async void Start()
        {
            DontDestroyOnLoad(gameObject);

            if (storeManager == null)
            {
                storeManager = FindObjectOfType<StoreManager>();
            }

            if (spinManager == null)
            {
                spinManager = FindObjectOfType<SpinRewardSystem>();
            }

            if (storeManager != null)
            {
                DontDestroyOnLoad(storeManager.gameObject);
            }

            if (spinManager != null)
            {
                DontDestroyOnLoad(spinManager.gameObject);
            }

            if (SceneService.Instance == null)
            {
                return;
            }

            if (!SceneService.Instance.IsSceneLoaded(SceneKeys.MenuScene))
            {
                await SceneService.Instance.LoadScene(SceneKeys.MenuScene);
            }
        }
    }
}
