using System;
using System.Threading.Tasks;
using GameLift.Audio;
using UnityEngine;
using Utils.LogicTimer;
using Utils.Scene;
using Game.Systems;
using Game.UI;
using VContainer;
using VContainer.Unity;
using Utils.Signal;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Installers
{
    public class GameInstaller : LifetimeScope, ISceneObject
    {
        [SerializeField] private Flipper _playerPrefab;
        [SerializeField] private ObstacleSettings _obstacleSettings;
        [SerializeField] private CollectableSettings _collectableSettings;
        [SerializeField] private CloudsConfig _cloudsConfig;
        [SerializeField] private GameCanvasSettings _gameCanvasSettings;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform collectableFlyDestination;
        [SerializeField] private Transform gameObjectsParent;
        [SerializeField] private CountdownController countdownController;
        public Canvas Canvas => canvas;
        public RectTransform CollectableFlyDestination => collectableFlyDestination;
        public Transform GameObjectsParent => gameObjectsParent;
        public CountdownController CountdownController => countdownController;

        public static GameInstaller Instance { get; private set; }

        private GameRuntimeEntryPoint _runtimeEntryPoint;
        private IDisposable _entryPointDisposable;

        protected override void Configure(IContainerBuilder builder)
        {
            Instance = this;

            if (gameObjectsParent != null)
            {
                builder.RegisterInstance(gameObjectsParent);
            }

            if (_cloudsConfig != null)
            {
                builder.RegisterInstance(_cloudsConfig);
            }

            builder.Register<CloudGeneratorSystem>(
                _ => new CloudGeneratorSystem(_cloudsConfig, gameObjectsParent),
                Lifetime.Singleton);

            if (IsCloudOnlyMode())
            {
                builder.RegisterEntryPoint<FlyGameCloudOnlyEntryPoint>(Lifetime.Singleton);
                return;
            }

            ResolveCountdownController();
            builder.RegisterInstance(countdownController);

            builder.Register<PlayerSystem>(
                resolver => new PlayerSystem(
                    _playerPrefab,
                    resolver.Resolve<GameLift.Audio.IAudioService>()),
                Lifetime.Singleton);
            builder.Register<ObstacleSystem>(
                _ => new ObstacleSystem(_obstacleSettings),
                Lifetime.Singleton);
            builder.Register<CollectableSystem>(
                resolver => new CollectableSystem(
                    _collectableSettings,
                    resolver.Resolve<GameLift.Audio.IAudioService>()),
                Lifetime.Singleton);
            builder.Register<GameCanvas>(
                _ => new GameCanvas(_gameCanvasSettings),
                Lifetime.Singleton);

            builder.RegisterEntryPoint<GameRuntimeEntryPoint>(Lifetime.Singleton);
        }

        internal void SetRuntimeEntryPoint(GameRuntimeEntryPoint runtimeEntryPoint)
        {
            _runtimeEntryPoint = runtimeEntryPoint;
            _entryPointDisposable = runtimeEntryPoint;
        }

        internal void SetEntryPointDisposable(IDisposable entryPoint)
        {
            _entryPointDisposable = entryPoint;
        }

        public Task Initialize() => Task.CompletedTask;

        public Task Clear()
        {
            _entryPointDisposable?.Dispose();
            _entryPointDisposable = null;
            _runtimeEntryPoint = null;
            return Task.CompletedTask;
        }

        public Task<bool> RestartGameplay()
        {
            return _runtimeEntryPoint?.RestartGameplay() ?? Task.FromResult(false);
        }

        public bool RequestStartGameplay()
        {
            if (_runtimeEntryPoint == null)
            {
                return false;
            }

            _runtimeEntryPoint.RequestStartGameplay();
            return true;
        }

        public void RestartToMenu()
        {
            _runtimeEntryPoint?.RestartToMenu();
        }

        private void OnApplicationPause(bool pause)
        {
            _runtimeEntryPoint?.HandlePause(pause);
        }

        private bool IsCloudOnlyMode()
        {
            return _playerPrefab == null
                && _obstacleSettings == null
                && _collectableSettings == null
                && _gameCanvasSettings == null
                && canvas == null
                && collectableFlyDestination == null;
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
    }

    public sealed class FlyGameCloudOnlyEntryPoint : IStartable, IFixedTickable, IDisposable
    {
        private readonly GameInstaller _installer;
        private readonly CloudGeneratorSystem _cloudGeneratorSystem;

        private LogicTimer _logicTimer;
        private bool _disposed;

        public FlyGameCloudOnlyEntryPoint(
            GameInstaller installer,
            CloudGeneratorSystem cloudGeneratorSystem)
        {
            _installer = installer;
            _cloudGeneratorSystem = cloudGeneratorSystem;
            _installer.SetEntryPointDisposable(this);
        }

        public void Start()
        {
            _logicTimer = new LogicTimer(() => _cloudGeneratorSystem.Tick());
            _logicTimer.Start();

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged += OnEditorPause;
#endif
        }

        public void FixedTick()
        {
            _logicTimer?.Update();
        }

        private void HandlePause(bool pause)
        {
            if (pause) _logicTimer?.Pause();
            else _logicTimer?.Resume();
        }

#if UNITY_EDITOR
        private void OnEditorPause(PauseState pauseState)
        {
            HandlePause(pauseState == PauseState.Paused);
        }
#endif

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged -= OnEditorPause;
#endif

            _logicTimer?.Dispose();
            _logicTimer = null;
            _cloudGeneratorSystem.Dispose();
        }
    }

    public sealed class GameRuntimeEntryPoint : IStartable, IFixedTickable, IDisposable
    {
        private readonly GameInstaller _installer;
        private readonly GameState _gameState;
        private readonly PlayerSystem _playerSystem;
        private readonly ObstacleSystem _obstacleSystem;
        private readonly CollectableSystem _collectableSystem;
        private readonly CloudGeneratorSystem _cloudGeneratorSystem;
        private readonly GameCanvas _gameCanvas;
        private readonly EnergyService _energyService;
        private readonly CountdownController _countdownController;

        private LogicTimer _logicTimer;
        private bool _disposed;
        private bool _gameStarted;
        private bool _gameStartInProgress;
        private int _gameStartRequestVersion;
        private Flipper _player;

        public GameRuntimeEntryPoint(
            GameInstaller installer,
            GameState gameState,
            PlayerSystem playerSystem,
            ObstacleSystem obstacleSystem,
            CollectableSystem collectableSystem,
            CloudGeneratorSystem cloudGeneratorSystem,
            GameCanvas gameCanvas,
            EnergyService energyService,
            IAudioService audioService,
            CountdownController countdownController)
        {
            _installer = installer;
            _gameState = gameState;
            _playerSystem = playerSystem;
            _obstacleSystem = obstacleSystem;
            _collectableSystem = collectableSystem;
            _cloudGeneratorSystem = cloudGeneratorSystem;
            _gameCanvas = gameCanvas;
            _energyService = energyService;
            _countdownController = countdownController;
            _countdownController?.SetAudioService(audioService);
            _installer.SetRuntimeEntryPoint(this);
        }

        public void Start()
        {
            _gameState.ConsumeImmediateGameStartRequest();
            BeginGame();

            _logicTimer = new LogicTimer(OnLogicTick);
            _logicTimer.Start();

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged += OnEditorPause;
#endif
        }

        public void FixedTick()
        {
            _logicTimer?.Update();
        }

        private void OnLogicTick()
        {
            _cloudGeneratorSystem.Tick();

            if (!_gameStarted)
            {
                return;
            }

            if (_gameState.CurrentState != GameFlowState.InGame)
            {
                _gameStarted = false;
                return;
            }

            _playerSystem.Tick();
            _obstacleSystem.Tick();
            _collectableSystem.Tick();
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
            if (_gameStarted || _gameStartInProgress)
            {
                return false;
            }

            _player = ResetGameplayObjects(stopMovers: true);
            if (_player == null)
            {
                Debug.LogWarning("[GameRuntime] Oyun başlatılamadı: player bulunamadı.");
                return false;
            }

            _gameStartInProgress = true;
            int requestVersion = ++_gameStartRequestVersion;
            bool energyAlreadySpent = _gameState.ConsumeNextGameStartEnergySpent();
            bool canStart;
            try
            {
                var energyTask = energyAlreadySpent
                    ? Task.FromResult(true)
                    : _energyService.TrySpendForRunStartAsync();
                var countdownTask = _countdownController != null
                    ? _countdownController.PlayAsync()
                    : Task.CompletedTask;

                await Task.WhenAll(energyTask, countdownTask);
                canStart = energyTask.Result;
            }
            finally
            {
                if (requestVersion == _gameStartRequestVersion)
                {
                    _gameStartInProgress = false;
                }
            }

            if (_disposed || requestVersion != _gameStartRequestVersion)
            {
                return false;
            }

            _countdownController?.Hide();

            if (!canStart)
            {
                PrepareForMenuStart(GameFlowState.WaitingToStart);
                NoEnergyPopUp.Show();
                return false;
            }

            _gameStarted = true;
            _countdownController?.PlayCountdownOverSound();
            SignalBus.Get<GameplayStartedSignal>().Invoke();

            return true;
        }

        internal Task<bool> RestartGameplay()
        {
            if (_disposed)
            {
                return Task.FromResult(false);
            }

            return TryBeginGameAsync();
        }

        internal void RestartToMenu()
        {
            if (_disposed)
            {
                return;
            }

            PrepareForMenuStart(GameFlowState.Menu);
            MenuCurrencyRewardFlyer.Instance?.ShowForMenu();
        }

        private void PrepareForMenuStart(GameFlowState state)
        {
            _player = ResetGameplayObjects(stopMovers: true);
            _player?.PrepareForStart();
            _gameState.SetState(state);
        }

        private Flipper ResetGameplayObjects(bool stopMovers)
        {
            _gameStartRequestVersion++;
            _gameStartInProgress = false;

            if (_player != null)
            {
                _player = null;
            }

            _gameStarted = false;

            _obstacleSystem.ResetForRestart(stopMovers);
            _collectableSystem.ResetForRestart(stopMovers);

            return _playerSystem.ResetForRestart(Vector3.zero, Quaternion.identity, _installer.GameObjectsParent);
        }

        internal void HandlePause(bool pause)
        {
            if (pause) _logicTimer?.Pause();
            else _logicTimer?.Resume();
        }

#if UNITY_EDITOR
        private void OnEditorPause(PauseState pauseState)
        {
            HandlePause(pauseState == PauseState.Paused);
        }
#endif

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged -= OnEditorPause;
#endif

            _logicTimer?.Dispose();
            _logicTimer = null;
            _countdownController?.Hide();

            if (_player != null)
            {
                _player = null;
            }

            _collectableSystem.Dispose();
            _obstacleSystem.Dispose();
            _cloudGeneratorSystem.Dispose();
            _playerSystem.Dispose();
            _gameCanvas.Dispose();
        }
    }
}
