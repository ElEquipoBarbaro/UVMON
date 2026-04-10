using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private List<InventoryItemStack> items = new List<InventoryItemStack>();
    [SerializeField] private List<UVmonInstance> uvmones = new List<UVmonInstance>();

    public event Action OnInventoryChanged;

    public List<InventoryItemStack> Items => items;
    public List<UVmonInstance> UVmones => uvmones;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddUVmon(UVmonData data, int level)
    {
        if (data == null) return;

        uvmones.Add(new UVmonInstance(data, level));
        NotifyInventoryChanged();
    }

    public void AddItem(ItemData data, int amount = 1)
    {
        if (data == null || amount <= 0) return;

        int remaining = amount;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].data == data && items[i].amount < data.maxStack)
            {
                int freeSpace = data.maxStack - items[i].amount;
                int toAdd = Mathf.Min(freeSpace, remaining);

                items[i].amount += toAdd;
                remaining -= toAdd;

                if (remaining <= 0)
                {
                    NotifyInventoryChanged();
                    return;
                }
            }
        }

        while (remaining > 0)
        {
            int toAdd = Mathf.Min(data.maxStack, remaining);
            items.Add(new InventoryItemStack(data, toAdd));
            remaining -= toAdd;
        }

        NotifyInventoryChanged();
    }

    public bool RemoveItem(ItemData data, int amount = 1)
    {
        if (data == null || amount <= 0) return false;

        int remaining = amount;

        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].data != data) continue;

            if (items[i].amount > remaining)
            {
                items[i].amount -= remaining;
                NotifyInventoryChanged();
                return true;
            }

            remaining -= items[i].amount;
            items.RemoveAt(i);

            if (remaining <= 0)
            {
                NotifyInventoryChanged();
                return true;
            }
        }

        NotifyInventoryChanged();
        return false;
    }

    public List<InventoryItemStack> GetItemsByCategory(ItemCategory category)
    {
        List<InventoryItemStack> result = new List<InventoryItemStack>();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].data != null && items[i].data.category == category)
            {
                result.Add(items[i]);
            }
        }

        return result;
    }

    public bool UseItem(InventoryItemStack stack)
    {
        if (stack == null || stack.data == null) return false;

        ItemData item = stack.data;

        if (item.category == ItemCategory.Healing && item.hpRestore > 0)
        {
            UVmonInstance target = FindFirstDamagedUVmon();

            if (target == null)
            {
                Debug.Log("No hay UVmones heridos.");
                return false;
            }

            target.currentHP = Mathf.Min(target.currentHP + item.hpRestore, target.MaxHP);

            if (item.consumable)
            {
                RemoveItem(item, 1);
            }
            else
            {
                NotifyInventoryChanged();
            }

            Debug.Log("Usaste " + item.itemName + " en " + target.data.uvmonName);
            return true;
        }

        if (item.category == ItemCategory.Combat)
        {
            if (item.consumable)
            {
                RemoveItem(item, 1);
            }
            else
            {
                NotifyInventoryChanged();
            }

            Debug.Log("Usaste " + item.itemName + ". Aquí conectas la lógica de combate.");
            return true;
        }

        Debug.Log(item.itemName + " todavía no tiene lógica especial.");
        return false;
    }

    private UVmonInstance FindFirstDamagedUVmon()
    {
        for (int i = 0; i < uvmones.Count; i++)
        {
            if (uvmones[i].currentHP < uvmones[i].MaxHP)
            {
                return uvmones[i];
            }
        }

        return null;
    }

    private void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}