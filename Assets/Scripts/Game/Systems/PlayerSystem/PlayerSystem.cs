using Game.Installers;
using System;
using UnityEngine;
using Utils.Logger;
using Utils.Pools;

namespace Game.Systems
{
    public class PlayerSystem : BaseSystem
    {
        private const int DefaultPoolCapacity = 25;
        private const int DefaultPoolPreload = 1;

        private readonly PlayerController playerPrefab;
        private Pool playerPool;

        private PlayerController currentPlayer;

        public static PlayerSystem Instance { get; private set; }

        public PlayerSystem(PlayerController playerPrefab, int preload = DefaultPoolPreload, int capacity = DefaultPoolCapacity)
        {
            if (Instance != null && Instance != this)
            {
                Instance.Dispose();
            }
            
            Instance = this;

            this.playerPrefab = playerPrefab;

            InitializePool(preload, capacity);
        }

        public override void Tick()
        {
            currentPlayer.Tick();
        }

        public PlayerController CreatePlayer()
        {
            return CreatePlayer(Vector3.zero, Quaternion.identity, GameInstaller.Instance.GameObjectsParent);
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

            currentPlayer = playerInstance;
            return playerInstance;
        }

        public PlayerController GetPlayer()
        {
            return currentPlayer;
        }

        public Transform GetPlayerTransform() 
        {
            return currentPlayer.transform;
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

        public override void Dispose()
        {
        }
    }
}
