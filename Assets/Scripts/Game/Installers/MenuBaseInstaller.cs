using Game.Systems;
using System;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Scene;
using VContainer;
using VContainer.Unity;

namespace Game.Installers
{
    public class MenuBaseInstaller : LifetimeScope, ISceneObject
    {
        [SerializeField] private SpinRewardSettings _spinRewardSettings;
        [SerializeField] private StoreSettings _storeSettings;

        public static MenuBaseInstaller Instance { get; private set; }

        private MenuRuntimeEntryPoint _runtimeEntryPoint;

        protected override void Configure(IContainerBuilder builder)
        {
            Instance = this;

            builder.RegisterInstance(_spinRewardSettings);
            builder.RegisterInstance(_storeSettings);
            if (_storeSettings != null && _storeSettings.ProductConfigs != null)
            {
                builder.RegisterInstance(_storeSettings.ProductConfigs);
            }

            builder.Register<SpinRewardSystem>(Lifetime.Singleton);
            builder.Register<StoreManager>(Lifetime.Singleton);

            builder.RegisterEntryPoint<MenuRuntimeEntryPoint>(Lifetime.Singleton);
        }

        internal void SetRuntimeEntryPoint(MenuRuntimeEntryPoint runtimeEntryPoint)
        {
            _runtimeEntryPoint = runtimeEntryPoint;
        }

        public Task Initialize() => Task.CompletedTask;

        public Task Clear()
        {
            _runtimeEntryPoint?.Dispose();
            _runtimeEntryPoint = null;
            return Task.CompletedTask;
        }
    }

    public sealed class MenuRuntimeEntryPoint : IStartable, IDisposable
    {
        private readonly MenuBaseInstaller _installer;
        private readonly ISceneService _sceneService;
        private readonly GameState _gameState;
        private readonly SpinRewardSystem _spinRewardSystem;
        private readonly StoreManager _storeManager;

        private bool _disposed;

        public MenuRuntimeEntryPoint(
            MenuBaseInstaller installer,
            ISceneService sceneService,
            GameState gameState,
            SpinRewardSystem spinRewardSystem,
            StoreManager storeManager)
        {
            _installer = installer;
            _sceneService = sceneService;
            _gameState = gameState;
            _spinRewardSystem = spinRewardSystem;
            _storeManager = storeManager;
            _installer.SetRuntimeEntryPoint(this);
        }

        public void Start()
        {
            _gameState.SetState(GameFlowState.Menu);
            _ = _sceneService.LoadScene(SceneKeys.MenuScene);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _storeManager.Dispose();
            _spinRewardSystem.Dispose();
        }
    }
}
