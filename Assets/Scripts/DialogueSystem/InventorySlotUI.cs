using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image rarityBorder;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button button;

    private InventoryItemStack currentStack;

    public void Setup(InventoryItemStack stack, Action<InventoryItemStack> onClick)
    {
        currentStack = stack;

        if (stack == null || stack.data == null) return;

        icon.sprite = stack.data.icon;
        nameText.text = stack.data.itemName;
        amountText.text = "x" + stack.amount;
        rarityBorder.color = GetRarityColor(stack.data.rarity);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(currentStack));
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Rare:
                return new Color(0.25f, 0.65f, 1f);
            case ItemRarity.Epic:
                return new Color(0.75f, 0.35f, 1f);
            case ItemRarity.Legendary:
                return new Color(1f, 0.75f, 0.2f);
            default:
                return new Color(0.75f, 0.75f, 0.75f);
        }
    }
}