using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Fila de la pestana INVENTARIO del menu de combate (Prompt 3: solo lectura; Prompt 7:
/// clic para usar el objeto durante el turno). No tiene logica propia de inventario --
/// solo pinta lo que CombatInventoryUI le pasa y reemite el clic, mismo patron "dumb relay"
/// que UIInventoryItem/PokemonSlotUI. La validacion real (¿es turno del jugador? ¿el
/// objeto es realmente usable?) vive en CombatManager.
/// </summary>
public class CombatInventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI quantityText;

    public event Action<CombatInventorySlotUI> OnClicked;

    /// <summary>Indice REAL en InventorySO (no la posicion visual en la lista -- los slots
    /// vacios se omiten al pintar, asi que ambos pueden diferir).</summary>
    public int InventoryIndex { get; private set; }

    public void SetData(Sprite sprite, string itemName, int quantity, int inventoryIndex)
    {
        InventoryIndex = inventoryIndex;

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (nameText != null)
            nameText.text = itemName;

        if (quantityText != null)
            quantityText.text = "x" + quantity;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }
}
