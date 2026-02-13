using UnityEngine;

namespace Game.Systems
{
    public class StoreManager : BaseSystem
    {
        private StoreSettings storeSettings;
        public StoreSettings StoreSettings => storeSettings;
        public static StoreManager Instance { get; private set; }

        public StoreManager(StoreSettings storeSettings)
        {
            if (Instance != null && Instance != this)
            {
                Instance.Dispose();
            }

            Instance = this;

            this.storeSettings = storeSettings;
        }
        public override void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public override void Tick()
        {
            throw new System.NotImplementedException();
        }
    }
}