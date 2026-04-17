using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "UVmon/Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Info básica")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Clasificación")]
    public ItemCategory category;
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Inventario")]
    [Min(1)] public int maxStack = 99;
    public bool consumable = true;

    [Header("Uso")]
    public bool usableOutsideBattle = true;
    public bool usableInBattle = true;

    [Header("Efectos simples")]
    public int hpRestore = 0;

    [Header("Economía")]
    public int sellPrice = 10;
}