using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicador flotante "FALLO" mostrado sobre un objetivo cuando un ataque no impacta.
/// Animacion tipo "drop-up": entra rapido (sube ligeramente + escala/alpha de 0 a 1),
/// se mantiene un instante visible y se desvanece — luego se autodestruye. No deja
/// GameObjects residuales en la jerarquia. Ver CLAUDE.md / COMBAT_SYSTEM_ANALYSIS.md.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MissIndicator : MonoBehaviour
{
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private Image image;

    [Header("Entrada (drop-up)")]
    [SerializeField] private float enterDuration = 0.15f;
    [SerializeField] private float dropDistance = 20f;
    [SerializeField] private float startScale = 0.6f;

    [Header("Visible")]
    [SerializeField] private float holdDuration = 0.4f;

    [Header("Salida")]
    [SerializeField] private float exitDuration = 0.15f;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = GetComponent<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();
    }

    private void Start()
    {
        StartCoroutine(PlayAndDestroy());
    }

    private IEnumerator PlayAndDestroy()
    {
        if (visualRoot == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Vector2 finalPos = visualRoot.anchoredPosition;
        Vector2 startPos = finalPos + new Vector2(0f, -dropDistance);

        // Entrada: sube + escala + alpha de 0 a 1, rapido.
        float elapsed = 0f;
        while (elapsed < enterDuration)
        {
            elapsed += Time.deltaTime;
            float t = enterDuration > 0f ? Mathf.Clamp01(elapsed / enterDuration) : 1f;

            visualRoot.anchoredPosition = Vector2.Lerp(startPos, finalPos, t);
            visualRoot.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, t);
            SetAlpha(Mathf.Lerp(0f, 1f, t));

            yield return null;
        }

        visualRoot.anchoredPosition = finalPos;
        visualRoot.localScale = Vector3.one;
        SetAlpha(1f);

        // Visible.
        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        // Salida: fade out.
        elapsed = 0f;
        while (elapsed < exitDuration)
        {
            elapsed += Time.deltaTime;
            float t = exitDuration > 0f ? Mathf.Clamp01(elapsed / exitDuration) : 1f;
            SetAlpha(Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        if (image == null)
            return;

        Color c = image.color;
        image.color = new Color(c.r, c.g, c.b, alpha);
    }
}
