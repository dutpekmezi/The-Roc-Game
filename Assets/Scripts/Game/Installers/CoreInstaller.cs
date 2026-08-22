using Cysharp.Threading.Tasks;
using Game.Systems;
using System.Threading;
using UnityEngine;
using Utils.Buttons;
using Utils.Currency;
using Utils.ObjectFlowAnimator;
using Utils.Pools;
using Utils.Popup;
using Utils.Save;
using Utils.Scene;
using Utils.Signal;
using VContainer;
using VContainer.Unity;

namespace Game.Installers
{
    public class CoreInstaller : LifetimeScope
    {
        [SerializeField] private SceneServiceSettings sceneServiceSettings;
        [SerializeField] private CurrencyServiceSettings currencyServiceSettings;
        [SerializeField] private bool persistBetweenScenes = true;
        [SerializeField] private Canvas canvas;
        [SerializeField] private CollectableSettings collectableSettings;
        [SerializeField] private EnergySettings energySettings;
        [SerializeField] private GameLift.Audio.SoundData firestorePurchaseSound;

        [Header("Optional GameLift Package Services")]
        [SerializeField] private GameLift.Audio.AudioServiceSettings gameLiftAudioSettings;
        [SerializeField] private GameLift.Levels.LevelList gameLiftLevelList;
        [SerializeField] private GameLift.Ads.AdsSettings gameLiftAdsSettings;
        [SerializeField] private GameLift.Popup.PopupSettings gameLiftPopupSettings;
        [SerializeField] private Canvas gameLiftPopupCanvas;

        public Canvas Canvas =>  canvas;
        public bool PersistBetweenScenes => persistBetweenScenes;

        public static CoreInstaller Instance { get; private set; }

        protected override void Configure(IContainerBuilder builder)
        {
            Instance = this;
            SignalBus.Clear();

            builder.RegisterInstance(sceneServiceSettings);
            builder.RegisterInstance(currencyServiceSettings);
            builder.RegisterInstance(collectableSettings);
            builder.RegisterInstance(energySettings);
            builder.RegisterInstance(canvas);

#if UNITY_WEBGL && !UNITY_EDITOR
            builder.Register<ISaveHandler, WebGLPlayerPrefsSaveHandler>(Lifetime.Singleton);
#else
            builder.Register<ISaveHandler, EncryptedSaveHandler>(Lifetime.Singleton);
#endif
            builder.Register<ISaveService, SaveService>(Lifetime.Singleton);
            builder.Register<ISceneService>(_ => new SceneService(sceneServiceSettings, this), Lifetime.Singleton);
            builder.Register<global::FirestoreGameSecurityService>(
                resolver =>
                {
                    resolver.TryResolve<GameLift.Audio.IAudioService>(out var audioService);
                    return new global::FirestoreGameSecurityService(audioService, firestorePurchaseSound);
                },
                Lifetime.Singleton);
            builder.Register<CurrencyService>(Lifetime.Singleton).As<ICurrencyService>().AsSelf();
            builder.Register<GameState>(Lifetime.Singleton);
            builder.Register<EnergyService>(Lifetime.Singleton).AsSelf();
            RegisterOptionalGameLiftServices(builder);

            builder.RegisterComponentInHierarchy<PopupService>();
            builder.RegisterComponentInHierarchy<UIFlowAnimator>().As<IUIFlowAnimator>();
            builder.RegisterComponentInHierarchy<ButtonManager>();
            builder.RegisterEntryPoint<CoreStartupEntryPoint>(Lifetime.Singleton);
        }

        private void RegisterOptionalGameLiftServices(IContainerBuilder builder)
        {
            builder.Register<GameLift.Signal.ISignalBus, GameLift.Signal.SignalBus>(Lifetime.Singleton);
#if UNITY_WEBGL && !UNITY_EDITOR
            builder.Register<GameLift.Save.ISaveHandler, GameLift.Save.WebGLPlayerPrefsSaveHandler>(Lifetime.Singleton);
#else
            builder.Register<GameLift.Save.ISaveHandler, GameLift.Save.EncryptedSaveHandler>(Lifetime.Singleton);
#endif
            builder.Register<GameLift.Save.ISaveService, GameLift.Save.SaveService>(Lifetime.Singleton);

            if (gameLiftAudioSettings != null)
            {
                builder.RegisterInstance(gameLiftAudioSettings);
                builder.RegisterEntryPoint<GameLift.Audio.AudioService>(Lifetime.Singleton)
                    .As<GameLift.Audio.IAudioService>();
            }

            if (gameLiftLevelList != null)
            {
                builder.RegisterInstance(gameLiftLevelList);
                builder.Register<GameLift.Levels.LevelService<GameLift.Levels.BaseLevelData>>(Lifetime.Singleton);
            }

            if (gameLiftAdsSettings != null)
            {
                builder.RegisterInstance(gameLiftAdsSettings);
                builder.RegisterEntryPoint<GameLift.Ads.AdsService>(Lifetime.Singleton).AsSelf();
            }

            if (gameLiftPopupSettings != null)
            {
                builder.RegisterInstance(gameLiftPopupSettings);
                builder.Register<GameLift.Popup.IPopupService, GameLift.Popup.PopupService>(Lifetime.Singleton)
                    .WithParameter(gameLiftPopupCanvas != null ? gameLiftPopupCanvas : canvas);
            }
        }

        private sealed class CoreStartupEntryPoint : IAsyncStartable
        {
            private readonly CoreInstaller _installer;
            private readonly ISceneService _sceneService;
            private readonly CurrencyService _cloudCurrencyService;
            private readonly global::FirestoreGameSecurityService _firestoreService;
            private readonly EnergyService _energyService;
            private readonly GameState _gameState;

            public CoreStartupEntryPoint(
                CoreInstaller installer,
                ISaveService saveService,
                ISceneService sceneService,
                CurrencyService cloudCurrencyService,
                global::FirestoreGameSecurityService firestoreService,
                EnergyService energyService,
                GameState gameState)
            {
                _installer = installer;
                _sceneService = sceneService;
                _cloudCurrencyService = cloudCurrencyService;
                _firestoreService = firestoreService;
                _energyService = energyService;
                _gameState = gameState;
            }

            public async UniTask StartAsync(CancellationToken cancellation = default)
            {
                if (_installer.PersistBetweenScenes)
                {
                    UnityEngine.Object.DontDestroyOnLoad(_installer.gameObject);
                }

                _ = Pools.Instance;
                _gameState.SetState(GameFlowState.Menu);

                bool firebaseReady = await BootstrapFirebaseDataAsync(cancellation);

                if (cancellation.IsCancellationRequested || !firebaseReady)
                {
                    return;
                }

                await _sceneService.LoadScene(SceneKeys.MenuBaseScene);
            }

            private async UniTask<bool> BootstrapFirebaseDataAsync(CancellationToken cancellation)
            {
                if (_firestoreService == null)
                {
                    Debug.LogWarning("[CoreStartup] FirestoreGameSecurityService missing; Firebase bootstrap skipped.");
                    return false;
                }

                bool firebaseReady;
                try
                {
                    Debug.Log("[CoreStartup] Firebase bootstrap starting.");
                    firebaseReady = await _firestoreService.InitializeServiceAsync();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[CoreStartup] Firebase bootstrap failed: " + e.Message);
                    return false;
                }

                if (cancellation.IsCancellationRequested)
                {
                    return false;
                }

                if (!firebaseReady)
                {
                    Debug.LogWarning("[CoreStartup] Firebase/Google sign-in did not finish; menu load blocked.");
                    return false;
                }

                if (_cloudCurrencyService != null)
                {
                    try
                    {
                        bool currenciesLoaded = await _cloudCurrencyService.RefreshFromFirebaseAsync();
                        Debug.Log("[CoreStartup] Firebase currency bootstrap result: " + currenciesLoaded);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("[CoreStartup] Firebase currency bootstrap failed: " + e.Message);
                    }
                }

                if (cancellation.IsCancellationRequested)
                {
                    return false;
                }

                if (_energyService != null)
                {
                    try
                    {
                        await _energyService.InitializeFromFirebaseAsync(forceRefresh: true);
                        Debug.Log("[CoreStartup] Firebase energy bootstrap finished. energy=" + _energyService.CurrentEnergy);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("[CoreStartup] Firebase energy bootstrap failed: " + e.Message);
                    }
                }

                return true;
            }
        }
    }
}
