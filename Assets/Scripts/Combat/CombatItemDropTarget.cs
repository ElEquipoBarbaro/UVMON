using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drop target used only by the active player UVGMon while an inventory item
/// is being dragged during combat.
/// </summary>
public class CombatItemDropTarget : MonoBehaviour, IDropHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image targetGraphic;
    [SerializeField] private Color hoverColor = new Color(0.55f, 1f, 0.65f, 1f);

    private bool dropEnabled;
    private bool highlighted;
    private bool defaultRaycastTarget;
    private bool initialized;
    private Color colorBeforeHighlight = Color.white;

    public event Action OnItemDropped;

    private void Awake()
    {
        EnsureGraphic();
    }

    private void OnDisable()
    {
        SetDropEnabled(false);
    }

    public void SetDropEnabled(bool enabled)
    {
        EnsureGraphic();

        dropEnabled = enabled;
        SetHighlighted(false);

        if (targetGraphic != null)
            targetGraphic.raycastTarget = enabled || defaultRaycastTarget;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (dropEnabled)
            SetHighlighted(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (dropEnabled)
            SetHighlighted(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!dropEnabled)
            return;

        SetHighlighted(false);
        OnItemDropped?.Invoke();
    }

private void EnsureGraphic()
    {
        if (initialized)
            return;

        if (targetGraphic == null)
            targetGraphic = GetComponent<Image>();

        if (targetGraphic != null)
            defaultRaycastTarget = targetGraphic.raycastTarget;

        initialized = true;
    }

    private void SetHighlighted(bool value)
    {
        if (targetGraphic == null || highlighted == value)
            return;

        if (value)
        {
            colorBeforeHighlight = targetGraphic.color;
            Color highlightedColor = hoverColor;
            highlightedColor.a = colorBeforeHighlight.a;
            targetGraphic.color = highlightedColor;
        }
        else
        {
            targetGraphic.color = colorBeforeHighlight;
        }

        highlighted = value;
    }
}
