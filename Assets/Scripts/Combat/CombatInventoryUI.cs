using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representacion visual del inventario REAL del jugador dentro del menu de combate
/// (Prompt 3: solo lectura; Prompt 7: clic para usar). No posee datos propios: cada
/// Refresh() lee InventorySO.GetCurrentInventoryState() -- el mismo InventorySO que usa
/// InventoryController/UIInventoryPage fuera de combate (ver
/// MenuInventary/MENU_INVENTORY_ANALYSIS.md) -- y solo instancia filas para pintarlo.
/// Nunca agrega/quita/usa items por su cuenta -- eso lo decide CombatManager (turno,
/// categoria valida) y lo ejecuta InventorySO/ItemEffect, exactamente como ya hace
/// InventoryController.HandleItemActionRequest fuera de combate.
/// </summary>
public class CombatInventoryUI : MonoBehaviour
{
    [SerializeField] private InventorySO inventoryData;
    [SerializeField] private Transform contentContainer;
    [SerializeField] private CombatInventorySlotUI slotPrefab;
    [SerializeField] private GameObject emptyStateLabel;

    private readonly List<CombatInventorySlotUI> spawnedSlots = new List<CombatInventorySlotUI>();

    /// <summary>El jugador hizo clic sobre un slot de inventario no vacio (Prompt 7).
    /// Indice REAL en InventorySO. CombatManager decide si es usable ahora mismo.</summary>
    public event Action<int> OnItemSelected;

    private void OnEnable()
    {
        if (inventoryData != null)
            inventoryData.OnInventoryUpdated += HandleInventoryUpdated;

        Refresh();
    }

    private void OnDisable()
    {
        if (inventoryData != null)
            inventoryData.OnInventoryUpdated -= HandleInventoryUpdated;
    }

    private void HandleInventoryUpdated(Dictionary<int, InventoryItem> state)
    {
        Repaint(state);
    }

    /// <summary>Fuerza una relectura del inventario real (p.ej. al abrir la pestana).</summary>
    public void Refresh()
    {
        if (inventoryData == null)
        {
            Repaint(null);
            return;
        }

        Repaint(inventoryData.GetCurrentInventoryState());
    }

    private void Repaint(Dictionary<int, InventoryItem> state)
    {
        foreach (CombatInventorySlotUI slot in spawnedSlots)
        {
            if (slot != null)
            {
                slot.OnClicked -= HandleSlotClicked;
                Destroy(slot.gameObject);
            }
        }
        spawnedSlots.Clear();

        if (contentContainer == null || slotPrefab == null)
            return;

        bool hasAnyItem = false;

        if (state != null)
        {
            foreach (KeyValuePair<int, InventoryItem> entry in state)
            {
                InventoryItem invItem = entry.Value;
                if (invItem.IsEmpty)
                    continue;

                hasAnyItem = true;

                CombatInventorySlotUI slot = Instantiate(slotPrefab, contentContainer);
                slot.gameObject.SetActive(true);
                slot.SetData(invItem.item.ItemImage, invItem.item.Name, invItem.quantity, entry.Key);
                slot.OnClicked += HandleSlotClicked;
                spawnedSlots.Add(slot);
            }
        }

        if (emptyStateLabel != null)
            emptyStateLabel.SetActive(!hasAnyItem);
    }

    private void HandleSlotClicked(CombatInventorySlotUI slot)
    {
        OnItemSelected?.Invoke(slot.InventoryIndex);
    }
}
