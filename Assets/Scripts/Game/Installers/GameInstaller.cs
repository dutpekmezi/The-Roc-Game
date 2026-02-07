using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Utils.LogicTimer;
using Utils.Scene;
using Game.Systems;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Installers
{
    public class GameInstaller : MonoBehaviour, ISceneObject
    {
        private bool _initialized;

        private readonly List<IDisposable> _disposables = new();

            
        private readonly List<IInitializable> _initializables = new();

        private PlayerSystem _playerSystem;
        private ObstacleSystem _obstacleSystem;
        private CollectableSystem _collectableSystem;
        private GameCanvas _gameCanvas;
        private LogicTimer _logicTimer;

        [SerializeField] private PlayerController _playerPrefab;
        [SerializeField] private ObstacleSettings _obstacleSettings;
        [SerializeField] private CollectableSettings _collectableSettings;
        [SerializeField] private GameCanvasSettings _gameCanvasSettings;
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform collectableFlyDestination;
        [SerializeField] private Transform gameObjectsParent;
        public Canvas Canvas => canvas;
        public RectTransform CollectableFlyDestination => collectableFlyDestination;
        public Transform GameObjectsParent => gameObjectsParent;

        public static GameInstaller Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
            }

            Instance = this;

            Initialize();
        }

        public Task Initialize()
        {
            if (_initialized)
                return Task.CompletedTask;

            _initialized = true;

            _playerSystem = BindDisposable(new PlayerSystem(_playerPrefab));
            _obstacleSystem = BindDisposable(new ObstacleSystem(_obstacleSettings));
            _collectableSystem = BindDisposable(new CollectableSystem(_collectableSettings));
            _gameCanvas = BindDisposable(new GameCanvas(_gameCanvasSettings));

            GameState.Instance.SetState(GameFlowState.InGame);

            _logicTimer = BindDisposable(new LogicTimer(OnLogicTick));
            _logicTimer.Start();

            _playerSystem.CreatePlayer();

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged += OnEditorPause;
#endif

            return Task.CompletedTask;
        }

        private void FixedUpdate()
        {
            _logicTimer?.Update();
        }

        public Task Clear()
        {
            for (int i = 0; i < _initializables.Count; i++)
                _initializables[i].Dispose();
            _initializables.Clear();

            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged -= OnEditorPause;
#endif

            return Task.CompletedTask;
        }

        private void OnLogicTick()
        {
            _playerSystem.Tick();
            _obstacleSystem.Tick();
            _collectableSystem.Tick();
        }

        private T BindDisposable<T>(T obj)
        {
            if (obj is IDisposable disposable)
                _disposables.Add(disposable);

            return obj;
        }

        private T InitializeInitializable<T>(T initializable) where T : IInitializable
        {
            initializable.Initialize();
            _initializables.Add(initializable);
            return initializable;
        }

        private void OnApplicationPause(bool pause)
        {
            HandlePause(pause);
        }

#if UNITY_EDITOR
        private void OnEditorPause(PauseState pauseState)
        {
            HandlePause(pauseState == PauseState.Paused);
        }
#endif

        private void HandlePause(bool pause)
        {
            if (pause) _logicTimer?.Pause();
            else _logicTimer?.Resume();
        }
    }
}
