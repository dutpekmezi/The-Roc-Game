using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public class ResponsiveGridCellSizer : MonoBehaviour
{
    [SerializeField] private float referenceScreenWidth = 1284f;
    [SerializeField] private float referenceCellWidth = 600f;
    [SerializeField] private float aspectRatio = 3f;

    private GridLayoutGroup gridLayoutGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        CacheComponents();
        UpdateCellSize();
    }

    private void Start()
    {
        UpdateCellSize();
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateCellSize();
    }

    private void CacheComponents()
    {
        if (gridLayoutGroup == null)
        {
            gridLayoutGroup = GetComponent<GridLayoutGroup>();
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
    }

    private void UpdateCellSize()
    {
        if (gridLayoutGroup == null)
        {
            return;
        }

        float width = rectTransform != null ? rectTransform.rect.width : 0f;
        if (width <= 0f)
        {
            width = Screen.width;
        }

        if (referenceScreenWidth <= 0f)
        {
            return;
        }

        float scale = width / referenceScreenWidth;
        float cellWidth = referenceCellWidth * scale;
        float cellHeight = aspectRatio != 0f ? cellWidth / aspectRatio : cellWidth;

        gridLayoutGroup.cellSize = new Vector2(cellWidth, cellHeight);
    }
}
