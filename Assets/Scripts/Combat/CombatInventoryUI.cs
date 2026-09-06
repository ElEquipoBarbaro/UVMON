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
    [SerializeField] private MouseFollower mouseFollower;

    private readonly List<CombatInventorySlotUI> spawnedSlots = new List<CombatInventorySlotUI>();
    private bool inputEnabled;
    private int draggedInventoryIndex = -1;

    /// <summary>El jugador hizo clic sobre un slot de inventario no vacio (Prompt 7).
    /// Indice REAL en InventorySO. CombatManager decide si es usable ahora mismo.</summary>
    public event Action<int> OnItemSelected;

    /// <summary>Notifica que hay un objeto siguiendo al cursor para habilitar al
    /// UVGmon activo como destino de drop.</summary>
    public event Action<int> OnItemDragStarted;
    public event Action OnItemDragEnded;

    private void OnEnable()
    {
        if (inventoryData != null)
            inventoryData.OnInventoryUpdated += HandleInventoryUpdated;

        Refresh();
    }

    private void OnDisable()
    {
        CancelDrag();

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
        // Un refresco puede destruir el slot que origino el drag. Cancelar primero
        // evita dejar el ghost o el destino de drop activos.
        CancelDrag();

        foreach (CombatInventorySlotUI slot in spawnedSlots)
        {
            if (slot != null)
            {
                slot.OnClicked -= HandleSlotClicked;
                slot.OnDragStarted -= HandleSlotDragStarted;
                slot.OnDragEnded -= HandleSlotDragEnded;
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
                slot.SetInteractable(inputEnabled);
                slot.OnClicked += HandleSlotClicked;
                slot.OnDragStarted += HandleSlotDragStarted;
                slot.OnDragEnded += HandleSlotDragEnded;
                spawnedSlots.Add(slot);
            }
        }

        if (emptyStateLabel != null)
            emptyStateLabel.SetActive(!hasAnyItem);
    }

    private void HandleSlotClicked(CombatInventorySlotUI slot)
    {
        if (!inputEnabled)
            return;

        OnItemSelected?.Invoke(slot.InventoryIndex);
    }

    private void HandleSlotDragStarted(CombatInventorySlotUI slot)
    {
        if (!inputEnabled || inventoryData == null || slot == null)
            return;

        int inventoryIndex = slot.InventoryIndex;
        if (!inventoryData.TryGetItemAt(inventoryIndex, out InventoryItem inventoryItem))
            return;

        CancelDrag();
        draggedInventoryIndex = inventoryIndex;

        // Toggle primero: el MouseFollower de batalla comienza inactivo y necesita
        // ejecutar Awake para resolver su UIInventoryItem antes de recibir los datos.
        if (mouseFollower != null)
        {
            mouseFollower.Toggle(true);
            mouseFollower.SetData(inventoryItem.item.ItemImage, inventoryItem.quantity);
        }

        OnItemDragStarted?.Invoke(inventoryIndex);
    }

    private void HandleSlotDragEnded(CombatInventorySlotUI slot)
    {
        CancelDrag();
    }

    /// <summary>
    /// Finaliza un drop valido y devuelve el indice real a consumir. Se limpia antes
    /// de que InventorySO emita OnInventoryUpdated y destruya el slot de origen.
    /// </summary>
    public int CompleteDrag()
    {
        if (draggedInventoryIndex < 0)
            return -1;

        int completedIndex = draggedInventoryIndex;
        draggedInventoryIndex = -1;

        if (mouseFollower != null)
            mouseFollower.Toggle(false);

        OnItemDragEnded?.Invoke();
        return completedIndex;
    }

    public void CancelDrag()
    {
        if (draggedInventoryIndex < 0)
            return;

        draggedInventoryIndex = -1;

        if (mouseFollower != null)
            mouseFollower.Toggle(false);

        OnItemDragEnded?.Invoke();
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled)
            CancelDrag();

        foreach (CombatInventorySlotUI slot in spawnedSlots)
            slot.SetInteractable(enabled);
    }
}
