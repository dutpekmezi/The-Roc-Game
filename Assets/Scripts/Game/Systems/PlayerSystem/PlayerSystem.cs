using UnityEngine;
using Utils.Logger;
using Utils.Pools;

namespace Game.Systems
{
    public class PlayerSystem
    {
        private const int DefaultPoolCapacity = 25;
        private const int DefaultPoolPreload = 1;

        private readonly PlayerData playerData;
        private readonly PlayerController playerPrefab;
        private Pool playerPool;

        public PlayerSystem(PlayerData playerData, PlayerController playerPrefab, int preload = DefaultPoolPreload, int capacity = DefaultPoolCapacity)
        {
            this.playerData = playerData;
            this.playerPrefab = playerPrefab;

            InitializePool(preload, capacity);
        }

        public PlayerController CreatePlayer()
        {
            return CreatePlayer(Vector3.zero, Quaternion.identity, null);
        }

        public PlayerController CreatePlayer(Vector3 position, Quaternion rotation, Transform parent)
        {
            if (playerPool == null)
            {
                InitializePool(DefaultPoolPreload, DefaultPoolCapacity);
            }

            if (playerPrefab == null)
            {
                GameLogger.LogWarning("PlayerSystem attempted to spawn a player without a prefab.");
                return null;
            }

            var playerInstance = Pools.Instance.Spawn(playerPrefab, position, rotation, parent);

            if (playerInstance == null)
            {
                GameLogger.LogWarning("PlayerSystem could not spawn a player (pool capacity reached?).");
                return null;
            }

            playerInstance.Init(playerData);

            return playerInstance;
        }

        private void InitializePool(int preload, int capacity)
        {
            if (playerPrefab == null)
            {
                GameLogger.LogWarning("PlayerSystem cannot initialize pool without a player prefab.");
                return;
            }

            if (capacity > 0)
            {
                playerPool = Pools.Instance.InitializePool(playerPrefab.gameObject, preload, capacity);
            }
            else
            {
                playerPool = Pools.Instance.InitializePool(playerPrefab.gameObject, preload);
            }
        }
    }
}
