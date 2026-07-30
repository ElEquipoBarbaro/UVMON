using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MoveOptionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Image background;

    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.08f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0.2f, 0.55f);

    [Header("Cursor")]
    [SerializeField] private Texture2D pointerCursor;
    [SerializeField] private Vector2 pointerCursorHotspot = Vector2.zero;

    public event Action<MoveOptionUI> OnHoverEnter;
    public event Action<MoveOptionUI> OnClicked;

    public int Index { get; private set; }

    public void SetIndex(int index)
    {
        Index = index;
    }

    public void SetText(string text)
    {
        if (label != null)
            label.text = text;
    }

    // El fondo se mantiene siempre habilitado (es el mismo Image que recibe el
    // raycast de hover/clic); resaltar solo cambia su color, para no perder el
    // area clickeable al des-resaltar.
    public void SetHighlighted(bool highlighted)
    {
        if (background != null)
            background.color = highlighted ? highlightColor : idleColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEnter?.Invoke(this);

        if (pointerCursor != null)
            Cursor.SetCursor(pointerCursor, pointerCursorHotspot, CursorMode.Auto);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (pointerCursor != null)
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }
}
