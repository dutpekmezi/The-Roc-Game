# Game Lift

A comprehensive Unity game development framework for mobile games. Game Lift provides a collection of ready-to-use services including scene management, popups, currency, save system, audio, ads, in-app purchases, and more — all wired together with dependency injection.

## Requirements

- Unity 6000.0.68f1 or higher
- [VContainer](https://vcontainer.hadashikick.jp/) 1.17.0
- [UniTask](https://github.com/Cysharp/UniTask) 2.5.10
- [DOTween](http://dotween.demigiant.com/)
- [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest) 2.8.1

## How to Install

### Install via Git URL (UPM)

1. Open the Unity Editor
2. Go to **Window > Package Manager**
3. Click the **+** button in the top-left corner
4. Select **Add package from git URL...**
5. Paste the repository URL and click **Add**

### Install from Local Folder

1. Clone or download this repository
2. Open the Unity Editor
3. Go to **Window > Package Manager**
4. Click the **+** button and select **Add package from disk...**
5. Navigate to the cloned folder and select `Assets/package.json`

### Install Required Dependencies

Game Lift depends on the following packages. Add them to your `Packages/manifest.json` if they are not already present:

```json
{
  "name": "package.openupm.com",
  "url": "https://package.openupm.com",
  "scopes": [
      "com.cysharp.unitask",
      "jp.hadashikick.vcontainer"
  ]
}
```
Import from Asset Store;
```json
"Dotween": "https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676", 
"NaughtyAttributes": "https://assetstore.unity.com/packages/tools/utilities/naughtyattributes-129996"
```
Import from Unity Packages;
```json
"Addressables",
"Ads Mediation",
"In-App Purchasing"
```
Import from Github;
```json
"Vibration": "https://github.com/BenoitFreslon/Vibration.git",
"UI Particle":"https://github.com/mob-sakai/ParticleEffectForUGUI.git"
```

## How to Setup

### Step 0: Import Starter Sample from Package Manager

Import Sample named Starter from Package Manager Game Lift Package

### Step 1: Assign a GameLiftLifetimeScope if it is not registered

Create VContainerSettings on Asset Create menu and assign GameLiftLifetimeScope prefab in Starter/Prefabs to VContainerSettings RootLifetimeScope field

### Step 2: Configure ScriptableObjects

Create the required settings assets via **Assets > Create** or by duplicating the ones provided in the Starter folder:

- **PopupSettings** — register your popup prefabs (each must extend `PopupBase`)
- **AudioSettings** — configure audio channels and SFX pool size
- **PoolSettings** — define object pools with capacity and preload counts
- **CurrencyConfig** — define currency types (e.g. Gold, Gems) with icons and starting values
- **LevelData** — create level definitions loaded via Addressables

Assign these ScriptableObjects to the corresponding fields on your `LifetimeScope` GameObject.

### Step 3: Setup Scenes

1. Create a **root scene** that contains your `LifetimeScope` GameObject — this scene stays loaded for the entire session
2. Create additional scenes for gameplay, menus, etc. and mark them as Addressable assets
3. Use `ISceneService` to load and unload scenes at runtime

### Step 4: Setup Popups

1. Create a Canvas in your root scene dedicated to popups
2. For each popup, create a prefab with a script extending `PopupBase`
3. Add animation components (`ScaleComponent`, `FadeComponent`, `CanvasFadeComponent`) to control appear/disappear transitions
4. Register all popup prefabs in your `PopupSettings` asset
5. Assign the popup canvas as the parent in the `PopupService` configuration

## How to Use

### Scene Management

Load and unload scenes asynchronously using Addressable scene references:

```csharp
[Inject] private ISceneService _sceneService;

// Load a scene
await _sceneService.LoadScene("GameScene");

// Unload a scene
await _sceneService.UnloadScene("GameScene");
```

### Popups

Create and manage UI popups with built-in animation support:

```csharp
[Inject] private IPopupService _popupService;

// Show a popup by type
var popup = _popupService.Create<GameWinPopup>();

// Show a popup by ID
var popup = _popupService.Create("game_win_popup");

// Close a popup
popup.Disappear();
```

Custom popups extend `PopupBase`:

```csharp
public class MyPopup : PopupBase
{
    public override string PopupId => "my_popup";

    protected override void Awake()
    {
        base.Awake();
        // Setup button listeners, etc.
    }
}
```

### Currency

Track and modify multiple currency types with save persistence:

```csharp
[Inject] private ICurrencyService _currencyService;

// Get current balance
float gold = _currencyService.GetCurrency("gold");

// Add currency
_currencyService.ModifyCurrency("gold", 100);

// Check if the player can afford something
bool canBuy = _currencyService.CanPurchase("gold", 50);

// Attempt a purchase (deducts if affordable)
bool success = _currencyService.TryPurchase("gold", 50);
```

### Save System

Persist data using the repository pattern with optional encryption:

```csharp
[Inject] private ISaveService _saveService;

// Save a value
_saveService.GetRepository<int>("high_score").Save(9999);

// Load a value
int score = _saveService.GetRepository<int>("high_score").Load();
```

### Audio

Play music, SFX, and ambient audio:

```csharp
[Inject] private IAudioService _audioService;

// Play a sound effect by name
_audioService.PlaySFX("coin_collect");

// Play background music
_audioService.PlayMusic("main_theme");

// Stop music
_audioService.StopMusic();
```

### Feedback (Audio + Haptics)

Provide combined audio and haptic feedback:

```csharp
[Inject] private IFeedbackService _feedbackService;

// Trigger haptic feedback
_feedbackService.Haptic(HapticType.Success);

// Play feedback (audio + haptics combined)
_feedbackService.PlayFeedback("button_click");
```

### Object Pooling

Efficiently reuse GameObjects to avoid runtime allocations:

```csharp
[Inject] private IPools _pools;

// Spawn from pool
var obj = _pools.Spawn<MyComponent>();

// Return to pool
_pools.Despawn(obj);
```

### Signals (Event Bus)

Communicate between services and UI using a decoupled signal system:

```csharp
[Inject] private ISignalBus _signalBus;

// Subscribe to a signal
_signalBus.Get<OnCurrencyChangedSignal>().AddListener(OnCurrencyChanged);

// Emit a signal
_signalBus.Get<OnCurrencyChangedSignal>().Emit(currencyId, newAmount);

private void OnCurrencyChanged(string id, float amount)
{
    // Update UI
}
```

### Level Management

Track level progression with Addressable-based level data:

```csharp
[Inject] private LevelService<BaseLevelData> _levelService;

// Get current level data
var levelData = _levelService.GetCurrentLevelData();

// Complete the current level
_levelService.CompleteLevel();
```

### Button Manager

Globally control button interactivity:

```csharp
[Inject] private ButtonManager _buttonManager;

// Disable all buttons except one
_buttonManager.DisableAll(exceptId: "pause_button");

// Re-enable all buttons
_buttonManager.EnableAll();
```

## Project Structure

```
Assets/
  Runtime/
    AudioService/        # Music, SFX, and ambient audio
    Buttons/             # Global button management
    CurrencyService/     # Multi-currency tracking with persistence
    FeedbackService/     # Haptics and audio feedback
    File/                # File I/O utilities
    LevelService/        # Level progression and data loading
    ObjectFlowAnimator/  # Particle flow animations (flying coins, etc.)
    Pools/               # Object pooling system
    PopupService/        # Queue-based popup management with animations
    PurchasingService/   # In-app purchase integration
    AdsService/          # Ad mediation (LevelPlay/IronSource)
    SaveService/         # Encrypted save system
    SceneService/        # Addressable-based scene management
    Signal/              # Event bus (SignalBus)
  Samples/
    Starter/             # Example project with full integration
```

## License

See [LICENSE](LICENSE) for details.
