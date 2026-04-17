using System;
using UnityEngine;

[Serializable]
public class InventoryItemStack
{
    public ItemData data;
    public int amount;

    public InventoryItemStack(ItemData data, int amount)
    {
        this.data = data;
        this.amount = amount;
    }
}

[Serializable]
public class UVmonInstance
{
    public UVmonData data;
    public int level;
    public int currentHP;

    public int MaxHP
    {
        get
        {
            if (data == null) return 0;
            return data.baseMaxHP + (level * 5);
        }
    }

    public UVmonInstance(UVmonData data, int level)
    {
        this.data = data;
        this.level = level;
        currentHP = MaxHP;
    }
}