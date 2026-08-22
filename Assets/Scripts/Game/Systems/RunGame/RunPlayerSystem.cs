using GameLift.Audio;
using UnityEngine;
using Utils.Logger;
using Utils.Pools;
using Utils.Popup;
using Utils.Signal;

namespace Game.Systems
{
    public class RunPlayerSystem : BaseSystem
    {
        private const int DefaultPoolCapacity = 2;
        private const int DefaultPoolPreload = 1;

        private readonly Runner playerPrefab;
        private readonly IAudioService audioService;
        private readonly PopupService popupService;
        private readonly Transform playerParent;
        private readonly Vector3 startPosition;

        private Runner currentPlayer;
        private GameObject runtimeFallbackPlayerPrefab;

        public static RunPlayerSystem Instance { get; private set; }

        public RunPlayerSystem(
            Runner playerPrefab,
            IAudioService audioService,
            PopupService popupService,
            Transform playerParent,
            Vector3 startPosition)
        {
            if (Instance != null && Instance != this)
            {
                Instance.Dispose();
            }

            Instance = this;
            this.playerPrefab = playerPrefab;
            this.audioService = audioService;
            this.popupService = popupService;
            this.playerParent = playerParent;
            this.startPosition = startPosition;

            SignalBus.Get<GameplayStartedSignal>().Subscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Subscribe(HandleGameplayStopped);
            InitializePool();
        }

        public override void Tick()
        {
            currentPlayer?.Tick();
        }

        public Runner GetOrCreatePlayer()
        {
            var existingPlayer = playerParent != null
                ? playerParent.GetComponentInChildren<Runner>(true)
                : null;

            if (existingPlayer != null)
            {
                RegisterPlayer(existingPlayer);
                return existingPlayer;
            }

            return CreatePlayer();
        }

        public Transform GetPlayerTransform()
        {
            return currentPlayer != null ? currentPlayer.transform : null;
        }

        public Runner ResetForRestart()
        {
            if (currentPlayer == null || !currentPlayer.gameObject.activeSelf)
            {
                currentPlayer = null;
                return CreatePlayer();
            }

            Pools.Instance?.CancelDelayedDespawn(currentPlayer.gameObject);
            currentPlayer.transform.SetParent(playerParent, false);
            InitializePlayer(currentPlayer);
            currentPlayer.PrepareForStart(startPosition);
            return currentPlayer;
        }

        private Runner CreatePlayer()
        {
            var prefab = playerPrefab != null ? playerPrefab.gameObject : CreateFallbackPlayerPrefab();
            if (prefab == null)
            {
                GameLogger.LogWarning("RunPlayerSystem attempted to spawn a player without a prefab.");
                return null;
            }

            GameObject instance = Pools.Instance.Spawn(
                prefab,
                startPosition,
                Quaternion.identity,
                playerParent);

            if (instance == null)
            {
                GameLogger.LogWarning("RunPlayerSystem could not spawn a player.");
                return null;
            }

            instance.SetActive(true);

            var player = instance.GetComponent<Runner>();
            if (player == null)
            {
                player = instance.AddComponent<Runner>();
            }

            RegisterPlayer(player);
            player.PrepareForStart(startPosition);
            return player;
        }

        private void RegisterPlayer(Runner player)
        {
            if (player == null)
            {
                return;
            }

            InitializePlayer(player);
            currentPlayer = player;
        }

        private void InitializePlayer(Runner player)
        {
            if (player == null)
            {
                return;
            }

            player.Initialize(
                audioService,
                popupService);
        }

        private void HandleGameplayStarted()
        {
            currentPlayer?.BeginGameplay(jumpOnStart: false);
        }

        private void HandleGameplayStopped()
        {
            currentPlayer?.StopGameplay();
        }

        private void InitializePool()
        {
            if (Pools.Instance == null)
            {
                return;
            }

            var prefab = playerPrefab != null ? playerPrefab.gameObject : CreateFallbackPlayerPrefab();
            if (prefab != null)
            {
                Pools.Instance.InitializePool(prefab, DefaultPoolPreload, DefaultPoolCapacity);
            }
        }

        private GameObject CreateFallbackPlayerPrefab()
        {
            if (runtimeFallbackPlayerPrefab != null)
            {
                return runtimeFallbackPlayerPrefab;
            }

            runtimeFallbackPlayerPrefab = new GameObject("RunBirdFallbackPrefab");
            runtimeFallbackPlayerPrefab.SetActive(false);

            var renderer = runtimeFallbackPlayerPrefab.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.WhiteSprite;
            renderer.color = new Color(0.18f, 0.56f, 0.24f, 1f);
            renderer.sortingOrder = 5;

            var collider = runtimeFallbackPlayerPrefab.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.75f, 1.15f);
            collider.isTrigger = false;
            var rb = runtimeFallbackPlayerPrefab.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            runtimeFallbackPlayerPrefab.AddComponent<Runner>();
            runtimeFallbackPlayerPrefab.transform.localScale = Vector3.one;
            Object.DontDestroyOnLoad(runtimeFallbackPlayerPrefab);
            return runtimeFallbackPlayerPrefab;
        }

        public override void Dispose()
        {
            SignalBus.Get<GameplayStartedSignal>().Unsubscribe(HandleGameplayStarted);
            SignalBus.Get<GameplayStoppedSignal>().Unsubscribe(HandleGameplayStopped);
            currentPlayer = null;

            if (runtimeFallbackPlayerPrefab != null)
            {
                Object.Destroy(runtimeFallbackPlayerPrefab);
                runtimeFallbackPlayerPrefab = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
