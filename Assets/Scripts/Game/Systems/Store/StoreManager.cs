using UnityEngine;

namespace Game.Systems
{
    public class StoreManager : MonoBehaviour
    {
        [SerializeField] private StoreSettings storeSettings;
        public StoreSettings StoreSettings => storeSettings;
        public static StoreManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance);
            }

            Instance = this;
        }
    }
}