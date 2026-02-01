using Game.Systems;
using TMPro;
using UnityEngine;
using Utils.Currency;
using Utils.Signal;

public class GameCanvas : MonoBehaviour
{
    [SerializeField] private Transform collectedCurrenciesCoffeeTransform;
    [SerializeField] private Transform collectedCurrenciesMatchaTransform;
    [SerializeField] private Transform collectedCurrenciesCoinTransform;

    [SerializeField] private TextMeshProUGUI collectedCoffeeCountText;
    [SerializeField] private TextMeshProUGUI collectedMatchaCountText;
    [SerializeField] private TextMeshProUGUI collectedCoinCountText;

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

    private void UpdateCollecteds(string currencyId, int amount)
    {
        switch (currencyId) 
        {
            case CurrencyIds.Coin:
                collectedCoinCountText.text = $"{amount}";
                break;
            case CurrencyIds.Coffee:
                collectedCoffeeCountText.text = $"{amount}";
                break;
            case CurrencyIds.Matcha:
                collectedMatchaCountText.text = $"{amount}";
                break;
            default: break;
        }
    }
}
