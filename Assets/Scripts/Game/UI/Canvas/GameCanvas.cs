using Game.Installers;
using Game.Systems;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using Utils.Logger;
using Utils.Pools;
using Utils.Signal;

public class GameCanvas : BaseSystem
{
    [SerializeField] private CollectableBar coffeeBar;
    [SerializeField] private CollectableBar matchaBar;
    [SerializeField] private CollectableBar coinBar;

    private GameCanvasSettings GameCanvasSettings;

    private Pool UIPool;

    private Transform collectableBarsParent;
    private List<CollectableBar> createdCollectableBars = new();

    public static GameCanvas Instance { get; private set; }

    public GameCanvas(GameCanvasSettings gameCanvasSettings)
    {
        if (Instance != null && Instance != this)
        {
            Instance.Dispose();
        }

        Instance = this;

        GameCanvasSettings = gameCanvasSettings;

        SignalBus.Get<CollectableSystem.CollectableCollected>().Subscribe(UpdateCollecteds);

        //CreateCollectableBars();
    }

    /*private void CreateCollectableBars()
    {
        //collectableBarsParent = Pools.Instance.Spawn(GameCanvasSettings.collectableBarsParent, GameInstaller.Instance.CollectableFlyDestination);

        foreach (var collectablebar in GameCanvasSettings.collectableBars)
        {
            var instance = Pools.Instance.Spawn(collectablebar, collectableBarsParent);
            createdCollectableBars.Add(instance);
        }
    }*/

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
            case var _ when IsSameCollectable(collectableConfig, coffeeBar?.CollectableConfig):
                coffeeBar.SetCount(amount);
                break;
            case var _ when IsSameCollectable(collectableConfig, matchaBar?.CollectableConfig):
                matchaBar.SetCount(amount);
                break;
            case var _ when IsSameCollectable(collectableConfig, coinBar?.CollectableConfig):
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

        if (coffeeBar != null && IsSameCollectable(coffeeBar.CollectableConfig, collectableConfig))
        {
            return coffeeBar;
        }

        if (matchaBar != null && IsSameCollectable(matchaBar.CollectableConfig, collectableConfig))
        {
            return matchaBar;
        }

        if (coinBar != null && IsSameCollectable(coinBar.CollectableConfig, collectableConfig))
        {
            return coinBar;
        }

        return null;
    }

    private static bool IsSameCollectable(CollectableConfig left, CollectableConfig right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left == right || (!string.IsNullOrEmpty(left.Id) && left.Id == right.Id);
    }

    public override void Tick()
    {
        throw new System.NotImplementedException();
    }

    private void InitializePool(int preload, int capacity, CollectableBar collectableBar)
    {
        if (GameCanvasSettings.collectableBars == null)
        {
            GameLogger.LogWarning("ObstacleSystem cannot initialize pool without a obstacle prefab.");
            return;
        }

        if (capacity > 0)
        {
            UIPool = Pools.Instance.InitializePool(collectableBar.gameObject, preload, capacity);
        }
        else
        {
            UIPool = Pools.Instance.InitializePool(collectableBar.gameObject, preload);
        }
    }

    public override void Dispose()
    {
        SignalBus.Get<CollectableSystem.CollectableCollected>().Unsubscribe(UpdateCollecteds);

        if (collectableBarsParent != null && createdCollectableBars != null)
        {
            foreach (var collectableBar in createdCollectableBars)
            {
                if (collectableBar != null) Pools.Instance.Despawn(collectableBar.gameObject);
            }

            createdCollectableBars.Clear();
            createdCollectableBars = null;
        }
    }
}
