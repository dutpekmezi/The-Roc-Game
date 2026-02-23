using Game.Systems;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Utils.LogicTimer;
using Utils.Scene;

namespace Game.Installers
{
    public class MenuBaseInstaller : MonoBehaviour, ISceneObject
    {
        private bool _initialized;

        private readonly List<IDisposable> _disposables = new();


        private readonly List<IInitializable> _initializables = new();

        private SpinRewardSystem _spinRewardSystem;
        private StoreManager _storeManager;

        [SerializeField] private SpinRewardSettings _spinRewardSettings;
        [SerializeField] private StoreSettings _storeSettings;

        public static MenuBaseInstaller Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            Initialize();
        }

        public Task Initialize()
        {
            if (_initialized)
                return Task.CompletedTask;

            _initialized = true;

            _spinRewardSystem = BindDisposable(new SpinRewardSystem(_spinRewardSettings));
            _storeManager = BindDisposable(new StoreManager(_storeSettings));

            GameState.Instance.SetState(GameFlowState.Menu);

            _ = SceneService.Instance.LoadScene(SceneKeys.MenuScene);

            /*_logicTimer = BindDisposable(new LogicTimer(OnLogicTick));
            _logicTimer.Start();*/


            return Task.CompletedTask;
        }

        public Task Clear()
        {
            for (int i = 0; i < _initializables.Count; i++)
                _initializables[i].Dispose();
            _initializables.Clear();

            for (int i = 0; i < _disposables.Count; i++)
                _disposables[i].Dispose();
            _disposables.Clear();

            return Task.CompletedTask;
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
    }
}
