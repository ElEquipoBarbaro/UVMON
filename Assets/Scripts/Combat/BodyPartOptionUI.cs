using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Un sprite de extremidad clickeable sobre la vista del enemigo (Prompt 18). Analogo a
/// MoveOptionUI pero para seleccionar el objetivo de un ataque en vez de un movimiento.
/// Un unico filtro blanco (flashOverlayImage) da feedback de hover, seleccion y golpe:
/// mismo efecto (parpadeo sinusoidal de alfa), solo cambia la velocidad y si es un bucle
/// continuo (hover/seleccion) o un pulso unico (golpe).
/// </summary>
[RequireComponent(typeof(Image))]
public class BodyPartOptionUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image image;
    [SerializeField] private Shadow selectionGlow;
    [SerializeField] private Image flashOverlayImage;

    [Header("Cursor")]
    [SerializeField] private Texture2D pointerCursor;
    [SerializeField] private Vector2 pointerCursorHotspot = Vector2.zero;

    [Header("Filtro blanco: hover/seleccion (bucle continuo, lento)")]
    [SerializeField] private float ambientBlinkSpeed = 4f;
    [SerializeField] private float ambientMaxAlpha = 0.6f;

    [Header("Filtro blanco: golpe recibido (pulso unico, rapido)")]
    [SerializeField] private float hitFlashDuration = 0.5f;
    [SerializeField] private float hitFlashSpeed = 18f;
    [SerializeField] private float hitFlashMaxAlpha = 0.85f;

    private static readonly Dictionary<Sprite, Sprite> whiteSpriteCache = new Dictionary<Sprite, Sprite>();

    public event Action<BodyPartOptionUI> OnClicked;

    public int Index { get; private set; }
    private bool isSelected;
    private bool isHovered;
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

        // El brillo de seleccion ahora lo da el filtro blanco (flashOverlayImage), no el
        // Shadow: si quedo uno de una version anterior de la escena, se neutraliza para
        // que no deje un borde fijo sin querer.
        if (selectionGlow == null)
            selectionGlow = GetComponent<Shadow>();

        if (selectionGlow != null)
            selectionGlow.effectColor = new Color(0f, 0f, 0f, 0f);

        if (flashOverlayImage != null)
        {
            flashOverlayImage.raycastTarget = false;
            flashOverlayImage.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private void Update()
    {
        // El pulso de golpe (rapido, un unico disparo) tiene prioridad exclusiva sobre el
        // filtro mientras esta corriendo: HitFlashRoutine ya escribe flashOverlayImage.color
        // cuadro a cuadro, asi que el bucle de hover/seleccion no debe pisarlo.
        if (flashOverlayImage == null || hitFlashRoutine != null)
            return;

        if (!isInteractable || !(isSelected || isHovered))
        {
            flashOverlayImage.color = new Color(1f, 1f, 1f, 0f);
            return;
        }

        float t = (Mathf.Sin(Time.unscaledTime * ambientBlinkSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(0f, ambientMaxAlpha, t);
        flashOverlayImage.color = new Color(1f, 1f, 1f, alpha);
    }

    public void SetIndex(int index)
    {
        Index = index;
    }

    /// <summary>Usado por EnemyBodyPartsView.FadeOut para desvanecer todas las partes juntas
    /// con un unico temporizador (ver PROMPT.md: fundido antes de iniciar la captura).</summary>
    public void SetAlpha(float alpha)
    {
        if (image == null)
            return;

        Color c = image.color;
        image.color = new Color(c.r, c.g, c.b, alpha);
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

    /// <summary>Marca esta parte como el objetivo actual: activa el filtro blanco en bucle.</summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
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
        {
            SetSelected(false);
            isHovered = false;
        }
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

        isHovered = true;

        if (pointerCursor != null)
            Cursor.SetCursor(pointerCursor, pointerCursorHotspot, CursorMode.Auto);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

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
