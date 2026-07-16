using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField] private string itemName;
    [SerializeField]
    [TextArea]
    private string description;
    [SerializeField] private Sprite itemImage;

    [Header("Inventory")]
    [SerializeField] private bool isStackable = true;
    [SerializeField] private int maxStackSize = 99;

    [Header("Effect")]
    [SerializeField] private ItemEffect effect;

    public string Name => itemName;
    public string Description => description;
    public Sprite ItemImage => itemImage;

    public bool IsStackable => isStackable;
    public int MaxStackSize => maxStackSize;

    public ItemEffect Effect => effect;

    public bool Use(CreatureRuntime target)
    {
        if (effect == null)
        {
            Debug.LogWarning($"{itemName} has no ItemEffect assigned.");
            return false;
        }

        return effect.Use(target);
    }
}