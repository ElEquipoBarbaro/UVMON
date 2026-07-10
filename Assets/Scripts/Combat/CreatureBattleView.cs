using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CreatureBattleView : MonoBehaviour
{
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private Image creatureImage;

    [Header("Motion Settings")]
    [SerializeField] private float shakePixels = 8f;
    [SerializeField] private float lungePixels = 18f;
    [SerializeField] private float chargePixels = 10f;
    [SerializeField] private float hopPixels = 10f;

    private Vector2 restingAnchoredPosition;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = GetComponent<RectTransform>();

        if (creatureImage == null)
            creatureImage = GetComponent<Image>();

        if (visualRoot != null)
            restingAnchoredPosition = visualRoot.anchoredPosition;
    }

    public void SetSprite(Sprite sprite)
    {
        if (creatureImage == null)
            return;

        creatureImage.sprite = sprite;
        creatureImage.enabled = sprite != null;
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