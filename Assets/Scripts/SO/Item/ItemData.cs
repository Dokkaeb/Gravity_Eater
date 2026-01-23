using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public ItemType itemType;
    public Sprite itemSprite;

    [Header("효과 설정")]
    public float duration;
    public float amount;

    [Header("프리팹")]
    public GameObject itemPrefab;
}
public enum ItemType
{
    Shield,
    Magnet,
    Boost
}
