using Game.Installers;
using GameLift.Audio;
using System;
using UnityEngine;
using Utils.Logger;
using Utils.Pools;
using Utils.Signal;

namespace Game.Systems
{
    public class PlayerSystem : BaseSystem
    {
        private const int DefaultPoolCapacity = 25;
        private const int DefaultPoolPreload = 1;

        private readonly Flipper playerPrefab;
        private readonly IAudioService audioService;
        private Pool playerPool;

        private Flipper currentPlayer;

        public static PlayerSystem Instance { get; private set; }

        public PlayerSystem(
            Flipper playerPrefab,
            IAudioService audioService,
            int preload = DefaultPoolPreload,
            int capacity = DefaultPoolCapacity)
        {
            if (Instance != null && Instance != this)
            {
                Instance.Dispose();
            }
            
            Instance = this;

            this.playerPrefab = playerPrefab;
            this.audioService = audioService;

            SignalBus.Get<GameplayStartedSignal>().Subscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Subscribe(HandleGameplayStopped);
            InitializePool(preload, capacity);
        }

        public override void Tick()
        {
            currentPlayer?.Tick();
        }

        public Flipper CreatePlayer()
        {
            return CreatePlayer(Vector3.zero, Quaternion.identity, GameInstaller.Instance.GameObjectsParent);
        }

        public Flipper CreatePlayer(Vector3 position, Quaternion rotation, Transform parent)
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

            playerInstance.Initialize(audioService);
            currentPlayer = playerInstance;
            return playerInstance;
        }

        public Flipper GetOrCreatePlayer(Transform parent)
        {
            var existingPlayer = parent != null
                ? parent.GetComponentInChildren<Flipper>(true)
                : null;

            if (existingPlayer != null)
            {
                RegisterPlayer(existingPlayer);
                return existingPlayer;
            }

            return CreatePlayer(Vector3.zero, Quaternion.identity, parent != null ? parent : GameInstaller.Instance.GameObjectsParent);
        }

        public void RegisterPlayer(Flipper player)
        {
            if (player == null)
            {
                return;
            }

            player.Initialize(audioService);
            currentPlayer = player;
        }

        public Flipper ResetForRestart()
        {
            return ResetForRestart(Vector3.zero, Quaternion.identity, GameInstaller.Instance != null
                ? GameInstaller.Instance.GameObjectsParent
                : null);
        }

        public Flipper ResetForRestart(Vector3 position, Quaternion rotation, Transform parent)
        {
            if (currentPlayer == null)
            {
                return CreatePlayer(position, rotation, parent);
            }

            if (!currentPlayer.gameObject.activeSelf)
            {
                currentPlayer = null;
                return CreatePlayer(position, rotation, parent);
            }

            Pools.Instance?.CancelDelayedDespawn(currentPlayer.gameObject);

            var playerTransform = currentPlayer.transform;
            playerTransform.SetParent(parent, false);
            playerTransform.localPosition = position;
            playerTransform.localRotation = rotation;

            currentPlayer.Initialize(audioService);
            currentPlayer.PrepareForStart();

            return currentPlayer;
        }

        public Flipper GetPlayer()
        {
            return currentPlayer;
        }

        public Transform GetPlayerTransform() 
        {
            return currentPlayer != null ? currentPlayer.transform : null;
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
            SignalBus.Get<GameplayStartedSignal>().Unsubscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Unsubscribe(HandleGameplayStopped);
            currentPlayer = null;
        }

        private void HandleGameplayStarted()
        {
            currentPlayer?.BeginGameplay(flapOnStart: false);
        }

        private void HandleGameplayStopped()
        {
            currentPlayer?.StopGameplay();
        }
    }
}
