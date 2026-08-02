using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Image flashOverlayImage;

    [Header("Brillo de seleccion (shadow blanco intermitente)")]
    [SerializeField] private Color glowColor = Color.white;
    [SerializeField] private float blinkSpeed = 4f;
    [SerializeField] private float minGlowAlpha = 0.15f;
    [SerializeField] private float maxGlowAlpha = 1f;
    [SerializeField] private Vector2 glowDistance = new Vector2(3f, -3f);

    [Header("Cursor")]
    [SerializeField] private Texture2D pointerCursor;
    [SerializeField] private Vector2 pointerCursorHotspot = Vector2.zero;

    [Header("Flash de golpe (filtro blanco intermitente sobre el sprite al recibir dano)")]
    [SerializeField] private float hitFlashDuration = 0.5f;
    [SerializeField] private float hitFlashSpeed = 18f;
    [SerializeField] private float hitFlashMaxAlpha = 0.85f;

    private static readonly Dictionary<Sprite, Sprite> whiteSpriteCache = new Dictionary<Sprite, Sprite>();

    public event Action<BodyPartOptionUI> OnClicked;

    public int Index { get; private set; }
    private bool isSelected;
    private bool isInteractable = true;
    private Coroutine hitFlashRoutine;

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

        if (flashOverlayImage != null)
        {
            flashOverlayImage.raycastTarget = false;
            flashOverlayImage.color = new Color(1f, 1f, 1f, 0f);
        }
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

        if (flashOverlayImage != null)
            flashOverlayImage.sprite = GetOrCreateWhiteSprite(sprite);
    }

    /// <summary>
    /// Genera (y cachea) una variante blanca de un sprite: mismo canal alfa, RGB en blanco
    /// puro. Se usa como overlay para el flash de golpe (Prompt 18: "filtro blanco...
    /// parpadea de manera intermitente y rapida"), en vez de solo desvanecer el alfa del
    /// sprite original. Requiere que la textura tenga Read/Write Enabled (ver CLAUDE.md).
    /// </summary>
    private static Sprite GetOrCreateWhiteSprite(Sprite source)
    {
        if (source == null || source.texture == null)
            return null;

        Sprite cached;
        if (whiteSpriteCache.TryGetValue(source, out cached) && cached != null)
            return cached;

        Rect r = source.rect;
        Color[] pixels = source.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color(1f, 1f, 1f, pixels[i].a);

        Texture2D whiteTex = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
        whiteTex.SetPixels(pixels);
        whiteTex.Apply();

        Vector2 normalizedPivot = new Vector2(source.pivot.x / r.width, source.pivot.y / r.height);
        Sprite whiteSprite = Sprite.Create(whiteTex, new Rect(0f, 0f, r.width, r.height), normalizedPivot, source.pixelsPerUnit);

        whiteSpriteCache[source] = whiteSprite;
        return whiteSprite;
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

    /// <summary>
    /// Habilita/deshabilita esta parte como objetivo seleccionable. Se usa cuando la
    /// extremidad llega a 0 de vida: deja de recibir clicks y de poder mostrarse como
    /// seleccionada (Prompt: "si ya se ataco una parte y su vida bajo a 0, no puedo
    /// volver a seleccionarla").
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;

        if (!interactable)
            SetSelected(false);
    }

    /// <summary>
    /// Feedback visual de golpe: superpone un filtro blanco (mismo silueta que el sprite,
    /// alfa a partir del canal alfa original) que parpadea de manera intermitente y rapida
    /// (patron sinusoidal) por un instante cuando el ataque impacta esta extremidad. El
    /// sprite base no se toca, asi que sus colores no se ven afectados entre parpadeos.
    /// </summary>
    public void PlayHitFlash()
    {
        if (flashOverlayImage == null || !gameObject.activeInHierarchy)
            return;

        if (hitFlashRoutine != null)
            StopCoroutine(hitFlashRoutine);

        hitFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        float elapsed = 0f;

        while (elapsed < hitFlashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = (Mathf.Sin(elapsed * hitFlashSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0f, hitFlashMaxAlpha, t);
            flashOverlayImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        flashOverlayImage.color = new Color(1f, 1f, 1f, 0f);
        hitFlashRoutine = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable)
            return;

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
        if (!isInteractable)
            return;

        OnClicked?.Invoke(this);
    }
}
