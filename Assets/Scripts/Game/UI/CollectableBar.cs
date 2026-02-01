using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectableBar : MonoBehaviour
{
    [SerializeField] private CollectableConfig collectableConfig;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;

    public CollectableConfig CollectableConfig => collectableConfig;
    public RectTransform IconRectTransform => iconImage != null ? iconImage.rectTransform : null;

    public void SetCollectableConfig(CollectableConfig config)
    {
        collectableConfig = config;
        InitializeFromConfig();
    }

    public void SetCount(int amount)
    {
        if (countText == null)
        {
            return;
        }

        countText.text = $"{amount}";
    }

    private void Awake()
    {
        InitializeReferences();
        InitializeFromConfig();
    }

    private void OnValidate()
    {
        InitializeReferences();
        InitializeFromConfig();
    }

    private void InitializeReferences()
    {
        if (iconImage == null)
        {
            iconImage = GetComponentInChildren<Image>(true);
        }

        if (countText == null)
        {
            countText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void InitializeFromConfig()
    {
        if (collectableConfig == null || iconImage == null)
        {
            return;
        }

        iconImage.sprite = collectableConfig.CollectableSprite;
    }
}
