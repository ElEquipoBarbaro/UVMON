using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CreatureBattleView : MonoBehaviour
{
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private Image creatureImage;
    [SerializeField] private Image flashOverlayImage;

    [Header("Motion Settings")]
    [SerializeField] private float shakePixels = 8f;
    [SerializeField] private float lungePixels = 18f;
    [SerializeField] private float chargePixels = 10f;
    [SerializeField] private float hopPixels = 10f;

    [Header("Filtro blanco: golpe recibido (mismos valores que BodyPartOptionUI)")]
    [SerializeField] private float hitFlashDuration = 0.5f;
    [SerializeField] private float hitFlashSpeed = 18f;
    [SerializeField] private float hitFlashMaxAlpha = 0.85f;

    private Vector2 restingAnchoredPosition;
    private Coroutine hitFlashRoutine;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = GetComponent<RectTransform>();

        if (creatureImage == null)
            creatureImage = GetComponent<Image>();

        if (visualRoot != null)
            restingAnchoredPosition = visualRoot.anchoredPosition;

        if (flashOverlayImage != null)
        {
            flashOverlayImage.raycastTarget = false;
            flashOverlayImage.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void SetSprite(Sprite sprite)
    {
        if (creatureImage == null)
            return;

        creatureImage.sprite = sprite;
        creatureImage.enabled = sprite != null;

        if (flashOverlayImage != null)
            flashOverlayImage.sprite = HitFlashEffect.GetOrCreateWhiteSprite(sprite);

        // Una nueva batalla reutiliza el mismo GameObject: si la anterior termino con un
        // fundido (ver FadeOut), hay que restaurar la opacidad para la proxima criatura.
        SetAlpha(1f);
    }

    public void SetAlpha(float alpha)
    {
        if (creatureImage == null)
            return;

        Color c = creatureImage.color;
        creatureImage.color = new Color(c.r, c.g, c.b, alpha);
    }

    /// <summary>Desvanece el sprite de la criatura (p.ej. antes de iniciar la captura).</summary>
    public IEnumerator FadeOut(float duration)
    {
        if (creatureImage == null)
            yield break;

        if (duration <= 0f)
        {
            SetAlpha(0f);
            yield break;
        }

        float startAlpha = creatureImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetAlpha(0f);
    }

    /// <summary>
    /// Feedback visual de golpe: mismo filtro blanco superpuesto (parpadeo sinusoidal
    /// unico, misma duracion/velocidad/alpha maximo) que usa BodyPartOptionUI para las
    /// extremidades del enemigo, aplicado al sprite completo. Usado tanto por el jugador
    /// como por el enemigo cuando no tiene partes de cuerpo. El sprite base no se toca —
    /// solo se anima el alpha del overlay — asi que nunca queda con un color/alpha
    /// incorrecto, ni siquiera con golpes consecutivos (reinicia la corrutina en curso).
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
        yield return HitFlashEffect.PlayOverlay(flashOverlayImage, hitFlashDuration, hitFlashSpeed, hitFlashMaxAlpha);
        hitFlashRoutine = null;
    }

    public void CacheRestingPosition()
    {
        if (visualRoot != null)
            restingAnchoredPosition = visualRoot.anchoredPosition;
    }

    public void ResetToRestingPosition()
    {
        if (visualRoot != null)
            visualRoot.anchoredPosition = restingAnchoredPosition;
    }

    public Vector2 CurrentAnchoredPosition
    {
        get
        {
            if (visualRoot != null)
                return visualRoot.anchoredPosition;

            return Vector2.zero;
        }
    }

    public IEnumerator PlayStartup(BattleMotionType motion, Vector2 attackDirection, float duration)
    {
        if (visualRoot == null || duration <= 0f)
            yield break;

        Vector2 origin = visualRoot.anchoredPosition;

        if (motion == BattleMotionType.None)
            yield break;

        Vector2 dir = attackDirection.sqrMagnitude > 0.0001f
            ? attackDirection.normalized
            : Vector2.right;

        Vector2 targetOffset = Vector2.zero;

        switch (motion)
        {
            case BattleMotionType.Shake:
                yield return ShakeMotion(origin, duration);
                yield break;

            case BattleMotionType.Lunge:
                targetOffset = dir * lungePixels;
                break;

            case BattleMotionType.Charge:
                targetOffset = -dir * chargePixels;
                break;

            case BattleMotionType.Hop:
                targetOffset = Vector2.up * hopPixels;
                break;
        }

        float elapsed = 0f;
        Vector2 peak = origin + targetOffset;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Out and back.
            float curve = t < 0.5f ? t * 2f : (1f - t) * 2f;
            visualRoot.anchoredPosition = Vector2.Lerp(origin, peak, curve);

            yield return null;
        }

        visualRoot.anchoredPosition = origin;
    }

    public IEnumerator PlayHitReaction(float duration)
    {
        if (visualRoot == null || duration <= 0f)
            yield break;

        Vector2 origin = visualRoot.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            Vector2 offset = Random.insideUnitCircle * (shakePixels * 0.35f);
            visualRoot.anchoredPosition = origin + offset;

            yield return null;
        }

        visualRoot.anchoredPosition = origin;
    }

    private IEnumerator ShakeMotion(Vector2 origin, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector2 offset = Random.insideUnitCircle * shakePixels;
            visualRoot.anchoredPosition = origin + offset;
            yield return null;
        }

        visualRoot.anchoredPosition = origin;
    }
}