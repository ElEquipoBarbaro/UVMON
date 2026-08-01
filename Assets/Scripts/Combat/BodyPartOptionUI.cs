using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Un sprite de extremidad clickeable sobre la vista del enemigo (Prompt 18). Analogo a
/// MoveOptionUI pero para seleccionar el objetivo de un ataque en vez de un movimiento.
/// Requiere un Shadow (blanco) en el mismo GameObject para el brillo intermitente de
/// seleccion; si no hay ninguno se agrega uno en tiempo de ejecucion.
/// </summary>
[RequireComponent(typeof(Image))]
public class BodyPartOptionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image image;
    [SerializeField] private Shadow selectionGlow;

    [Header("Brillo de seleccion (shadow blanco intermitente)")]
    [SerializeField] private Color glowColor = Color.white;
    [SerializeField] private float blinkSpeed = 4f;
    [SerializeField] private float minGlowAlpha = 0.15f;
    [SerializeField] private float maxGlowAlpha = 1f;
    [SerializeField] private Vector2 glowDistance = new Vector2(3f, -3f);

    [Header("Cursor")]
    [SerializeField] private Texture2D pointerCursor;
    [SerializeField] private Vector2 pointerCursorHotspot = Vector2.zero;

    public event Action<BodyPartOptionUI> OnClicked;

    public int Index { get; private set; }
    private bool isSelected;

    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();

        // El hit test por alpha permite que el click atraviese las zonas transparentes
        // del sprite (p.ej. la cabeza dentro del lienzo del cuerpo) y llegue a lo que
        // este debajo en la jerarquia.
        if (image != null)
            image.alphaHitTestMinimumThreshold = 0.1f;

        if (selectionGlow == null)
            selectionGlow = GetComponent<Shadow>();

        if (selectionGlow == null)
            selectionGlow = gameObject.AddComponent<Shadow>();

        selectionGlow.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        selectionGlow.effectDistance = glowDistance;
        selectionGlow.useGraphicAlpha = true;
    }

    private void Update()
    {
        if (!isSelected || selectionGlow == null)
            return;

        float t = (Mathf.Sin(Time.unscaledTime * blinkSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minGlowAlpha, maxGlowAlpha, t);
        selectionGlow.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
    }

    public void SetIndex(int index)
    {
        Index = index;
    }

    public void SetSprite(Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    public void SetAnchoredPosition(Vector2 position)
    {
        RectTransform rt = transform as RectTransform;
        if (rt != null)
            rt.anchoredPosition = position;
    }

    /// <summary>Marca esta parte como el objetivo actual: activa el brillo intermitente.</summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionGlow != null && !selected)
            selectionGlow.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
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
