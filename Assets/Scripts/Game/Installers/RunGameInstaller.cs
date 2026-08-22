using System;
using System.Threading.Tasks;
using GameLift.Audio;
using Game.Systems;
using Game.UI;
using UnityEngine;
using Utils.LogicTimer;
using Utils.Popup;
using Utils.Scene;
using Utils.Signal;
using VContainer;
using VContainer.Unity;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Installers
{
    public class RunGameInstaller : LifetimeScope, ISceneObject
    {
        [SerializeField] private Runner playerPrefab;
        [SerializeField] private RunObstacleSettings obstacleSettings;
        [SerializeField] private CloudsConfig cloudsConfig;
        [SerializeField] private RoadConfig roadConfig;
        [SerializeField] private ScoreConfig scoreConfig;
        [SerializeField] private CollectableSettings collectableSettings;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform collectableFlyDestination;
        [SerializeField] private Transform gameObjectsParent;
        [SerializeField] private Vector3 playerStartPosition = new Vector3(-3.25f, -3.55f, 0f);
        [SerializeField] private CountdownController countdownController;
        [SerializeField] private RoadGenerator roadGenerator;

        private RunGameRuntimeEntryPoint runtimeEntryPoint;

        public static RunGameInstaller Instance { get; private set; }
        public Transform GameObjectsParent => gameObjectsParent != null ? gameObjectsParent : transform;
        public Vector3 PlayerStartPosition => playerStartPosition;
        public CountdownController CountdownController => countdownController;
        public RoadGenerator RoadGenerator => roadGenerator;
        public ScoreConfig ScoreConfig => scoreConfig;
        public Canvas Canvas => canvas;
        public RectTransform CollectableFlyDestination => collectableFlyDestination;

        protected override void Configure(IContainerBuilder builder)
        {
            Instance = this;
            ResolveSceneReferences();
            ResolveCanvasReferences();
            ResolveCountdownController();
            ResolveRoadGenerator();

            builder.RegisterInstance(gameObjectsParent);
            builder.RegisterInstance(countdownController);
            builder.RegisterInstance(roadGenerator);

            if (cloudsConfig != null)
            {
                builder.RegisterInstance(cloudsConfig);
            }

            if (roadConfig != null)
            {
                builder.RegisterInstance(roadConfig);
            }

            if (scoreConfig != null)
            {
                builder.RegisterInstance(scoreConfig);
            }

            if (collectableSettings != null)
            {
                builder.RegisterInstance(collectableSettings);
            }

            if (obstacleSettings != null)
            {
                builder.RegisterInstance(obstacleSettings);
            }

            builder.Register<RunObstacleSystem>(
                _ => new RunObstacleSystem(obstacleSettings, GameObjectsParent),
                Lifetime.Singleton);

            builder.Register<CloudGeneratorSystem>(
                _ => new CloudGeneratorSystem(cloudsConfig, GameObjectsParent),
                Lifetime.Singleton);

            builder.Register<ScoreService>(
                resolver => new ScoreService(
                    scoreConfig,
                    resolver.Resolve<GameLift.Audio.IAudioService>()),
                Lifetime.Singleton);

            builder.Register<CollectableSystem>(
                resolver =>
                {
                    var resolvedCollectableSettings = collectableSettings != null
                        ? collectableSettings
                        : scoreConfig != null
                            ? scoreConfig.CollectableSettings
                            : null;

                    if (resolvedCollectableSettings == null)
                    {
                        resolver.TryResolve<CollectableSettings>(out resolvedCollectableSettings);
                    }

                    return new CollectableSystem(
                        resolvedCollectableSettings,
                        resolver.Resolve<GameLift.Audio.IAudioService>());
                },
                Lifetime.Singleton);

            builder.Register<RunPlayerSystem>(
                resolver =>
                {
                    resolver.TryResolve<PopupService>(out var popupService);
                    return new RunPlayerSystem(
                        playerPrefab,
                        resolver.Resolve<GameLift.Audio.IAudioService>(),
                        popupService,
                        GameObjectsParent,
                        playerStartPosition);
                },
                Lifetime.Singleton);

            builder.RegisterEntryPoint<RunGameRuntimeEntryPoint>(Lifetime.Singleton);
        }

        internal void SetRuntimeEntryPoint(RunGameRuntimeEntryPoint entryPoint)
        {
            runtimeEntryPoint = entryPoint;
        }

        public Task Initialize() => Task.CompletedTask;

        public Task Clear()
        {
            runtimeEntryPoint?.Dispose();
            runtimeEntryPoint = null;

            if (Instance == this)
            {
                Instance = null;
            }

            return Task.CompletedTask;
        }

        public Task<bool> RestartGameplay()
        {
            return runtimeEntryPoint?.RestartGameplay() ?? Task.FromResult(false);
        }

        public bool RequestStartGameplay()
        {
            if (runtimeEntryPoint == null)
            {
                return false;
            }

            runtimeEntryPoint.RequestStartGameplay();
            return true;
        }

        public void RestartToMenu()
        {
            runtimeEntryPoint?.RestartToMenu();
        }

        private void ResolveSceneReferences()
        {
            if (gameObjectsParent != null)
            {
                return;
            }

            var gameObjects = transform.root.Find("GameObjects");
            gameObjectsParent = gameObjects != null ? gameObjects : transform.root;
        }

        private void ResolveCanvasReferences()
        {
            if (canvas == null)
            {
                canvas = GetComponentInChildren<Canvas>(true);
            }

            if (collectableFlyDestination == null && canvas != null)
            {
                var destination = canvas.transform.Find("CollectableFlyDestination");
                collectableFlyDestination = destination != null
                    ? destination as RectTransform
                    : canvas.GetComponentInChildren<RectTransform>(true);
            }
        }

        private void ResolveCountdownController()
        {
            if (countdownController == null)
            {
                countdownController = GetComponentInChildren<CountdownController>(true);
            }

            if (countdownController == null)
            {
                var countdownObject = new GameObject("CountdownController");
                countdownObject.transform.SetParent(transform, false);
                countdownController = countdownObject.AddComponent<CountdownController>();
            }

            countdownController.Hide();
        }

        private void ResolveRoadGenerator()
        {
            if (roadGenerator == null)
            {
                roadGenerator = GetComponentInChildren<RoadGenerator>(true);
            }

            if (roadGenerator == null)
            {
                var roadObject = new GameObject("RoadGenerator");
                roadObject.transform.SetParent(transform, false);
                roadGenerator = roadObject.AddComponent<RoadGenerator>();
            }

            roadGenerator.SetConfig(roadConfig);
            roadGenerator.ResetForRestart(stopMovement: true);
        }

        private void OnApplicationPause(bool pause)
        {
            runtimeEntryPoint?.HandlePause(pause);
        }
    }

    public sealed class RunGameRuntimeEntryPoint : IStartable, IFixedTickable, IDisposable
    {
        private readonly RunGameInstaller installer;
        private readonly GameState gameState;
        private readonly RunPlayerSystem playerSystem;
        private readonly RunObstacleSystem obstacleSystem;
        private readonly CloudGeneratorSystem cloudGeneratorSystem;
        private readonly ScoreService scoreService;
        private readonly CollectableSystem collectableSystem;
        private readonly EnergyService energyService;
        private readonly CountdownController countdownController;
        private readonly RoadGenerator roadGenerator;

        private LogicTimer logicTimer;
        private Runner player;
        private bool disposed;
        private bool gameStarted;
        private bool gameStartInProgress;
        private int gameStartRequestVersion;

        public RunGameRuntimeEntryPoint(
            RunGameInstaller installer,
            GameState gameState,
            RunPlayerSystem playerSystem,
            RunObstacleSystem obstacleSystem,
            CloudGeneratorSystem cloudGeneratorSystem,
            ScoreService scoreService,
            CollectableSystem collectableSystem,
            EnergyService energyService,
            IAudioService audioService,
            CountdownController countdownController,
            RoadGenerator roadGenerator)
        {
            this.installer = installer;
            this.gameState = gameState;
            this.playerSystem = playerSystem;
            this.obstacleSystem = obstacleSystem;
            this.cloudGeneratorSystem = cloudGeneratorSystem;
            this.scoreService = scoreService;
            this.collectableSystem = collectableSystem;
            this.energyService = energyService;
            this.countdownController = countdownController;
            this.countdownController?.SetAudioService(audioService);
            this.roadGenerator = roadGenerator;
            installer.SetRuntimeEntryPoint(this);
        }

        public void Start()
        {
            gameState.ConsumeImmediateGameStartRequest();
            BeginGame();

            logicTimer = new LogicTimer(OnLogicTick);
            logicTimer.Start();

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged += OnEditorPause;
#endif
        }

        public void FixedTick()
        {
            logicTimer?.Update();
        }

        private void OnLogicTick()
        {
            cloudGeneratorSystem.Tick();

            if (!gameStarted)
            {
                return;
            }

            if (gameState.CurrentState != GameFlowState.InGame)
            {
                gameStarted = false;
                return;
            }

            playerSystem.Tick();
            obstacleSystem.Tick();
            roadGenerator?.Tick();
            scoreService.Tick();
            collectableSystem.Tick();
        }

        internal void RequestStartGameplay()
        {
            BeginGame();
        }

        private async void BeginGame()
        {
            await TryBeginGameAsync();
        }

        private async Task<bool> TryBeginGameAsync()
        {
            if (gameStarted || gameStartInProgress)
            {
                if (gameState.CurrentState == GameFlowState.InGame || gameStartInProgress)
                {
                    return false;
                }

                gameStarted = false;
            }

            player = ResetGameplayObjects(stopMovers: true);
            player?.PrepareForStart(installer.PlayerStartPosition);

            if (player == null)
            {
                Debug.LogWarning("[RunGame] Oyun baslatilamadi: player bulunamadi.");
                return false;
            }

            gameStartInProgress = true;
            int requestVersion = ++gameStartRequestVersion;
            bool energyAlreadySpent = gameState.ConsumeNextGameStartEnergySpent();
            bool canStart;

            try
            {
                var energyTask = energyAlreadySpent
                    ? Task.FromResult(true)
                    : energyService.TrySpendForRunStartAsync();
                var countdownTask = countdownController != null
                    ? countdownController.PlayAsync()
                    : Task.CompletedTask;

                await Task.WhenAll(energyTask, countdownTask);
                canStart = energyTask.Result;
            }
            finally
            {
                if (requestVersion == gameStartRequestVersion)
                {
                    gameStartInProgress = false;
                }
            }

            if (disposed || requestVersion != gameStartRequestVersion)
            {
                return false;
            }

            countdownController?.Hide();

            if (!canStart)
            {
                PrepareForMenuStart(GameFlowState.WaitingToStart);
                NoEnergyPopUp.Show();
                return false;
            }

            gameStarted = true;
            countdownController?.PlayCountdownOverSound();
            SignalBus.Get<GameplayStartedSignal>().Invoke();
            return true;
        }

        internal Task<bool> RestartGameplay()
        {
            if (disposed)
            {
                return Task.FromResult(false);
            }

            return TryBeginGameAsync();
        }

        internal void RestartToMenu()
        {
            if (disposed)
            {
                return;
            }

            PrepareForMenuStart(GameFlowState.Menu);
            MenuCurrencyRewardFlyer.Instance?.ShowForMenu();
        }

        private void PrepareForMenuStart(GameFlowState state)
        {
            player = ResetGameplayObjects(stopMovers: true);
            player?.PrepareForStart(installer.PlayerStartPosition);
            gameState.SetState(state);
        }

        private Runner ResetGameplayObjects(bool stopMovers)
        {
            gameStartRequestVersion++;
            gameStartInProgress = false;
            gameStarted = false;

            player = null;

            obstacleSystem.ResetForRestart(stopMovers);
            collectableSystem.ResetForRestart(stopMovers);
            roadGenerator?.ResetForRestart(stopMovers);
            scoreService.ResetScore();
            return playerSystem.ResetForRestart();
        }

        internal void HandlePause(bool pause)
        {
            if (pause)
            {
                logicTimer?.Pause();
            }
            else
            {
                logicTimer?.Resume();
            }
        }

#if UNITY_EDITOR
        private void OnEditorPause(PauseState pauseState)
        {
            HandlePause(pauseState == PauseState.Paused);
        }
#endif

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged -= OnEditorPause;
#endif

            logicTimer?.Dispose();
            logicTimer = null;
            countdownController?.Hide();

            player = null;

            obstacleSystem.Dispose();
            collectableSystem.Dispose();
            roadGenerator?.Dispose();
            cloudGeneratorSystem.Dispose();
            scoreService.Dispose();
            playerSystem.Dispose();
        }
    }
}
