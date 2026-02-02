using Game.Systems;
using UnityEngine;
using Utils.Signal;

public class GameCanvas : MonoBehaviour
{
    [SerializeField] private CollectableBar coffeeBar;
    [SerializeField] private CollectableBar matchaBar;
    [SerializeField] private CollectableBar coinBar;

    public static GameCanvas Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;
    }

    private void Start()
    {
        SignalBus.Get<CollectableSystem.CollectableCollected>().Subscribe(UpdateCollecteds);
    }

    public bool TryGetCollectableBarScreenPosition(CollectableConfig collectableConfig, out Vector3 screenPosition)
    {
        var bar = GetCollectableBar(collectableConfig);
        if (bar == null || bar.IconRectTransform == null)
        {
            screenPosition = Vector3.zero;
            return false;
        }

        screenPosition = RectTransformUtility.WorldToScreenPoint(null, bar.IconRectTransform.position);
        return true;
    }

    private void UpdateCollecteds(CollectableConfig collectableConfig, int amount)
    {
        switch (collectableConfig)
        {
            case var _ when coffeeBar != null && collectableConfig == coffeeBar.CollectableConfig:
                coffeeBar.SetCount(amount);
                break;
            case var _ when matchaBar != null && collectableConfig == matchaBar.CollectableConfig:
                matchaBar.SetCount(amount);
                break;
            case var _ when coinBar != null && collectableConfig == coinBar.CollectableConfig:
                coinBar.SetCount(amount);
                break;
            default:
                break;
        }
    }

    private CollectableBar GetCollectableBar(CollectableConfig collectableConfig)
    {
        if (collectableConfig == null)
        {
            return null;
        }

        if (coffeeBar != null && coffeeBar.CollectableConfig == collectableConfig)
        {
            return coffeeBar;
        }

        if (matchaBar != null && matchaBar.CollectableConfig == collectableConfig)
        {
            return matchaBar;
        }

        if (coinBar != null && coinBar.CollectableConfig == collectableConfig)
        {
            return coinBar;
        }

        return null;
    }
}
