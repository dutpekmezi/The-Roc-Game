using UnityEngine;

[CreateAssetMenu(fileName = "CollectableConfig", menuName = "Game/Collectable/Collectable Config", order = 1)]
public class CollectableConfig : ScriptableObject
{
    [SerializeField] private string collectableId;
    [SerializeField] private Sprite collectableSprite;

    public string CollectableId => collectableId;
    public Sprite CollectableSprite => collectableSprite;
}
