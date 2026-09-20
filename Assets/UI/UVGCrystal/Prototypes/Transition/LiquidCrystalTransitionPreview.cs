using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LiquidCrystalTransitionPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup transitionGroup;
    [SerializeField] private RectTransform spinner;
    [SerializeField] private Image[] spinnerSegments;
    [SerializeField] private TMP_Text loadingLabel;

    [Header("Motion")]
    [SerializeField, Min(0.1f)] private float fadeDuration = 0.8f;
    [SerializeField] private float rotationSpeed = -125f;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 2.8f;

    private float elapsed;

    private void OnEnable()
    {
        elapsed = 0f;

        if (transitionGroup != null)
        {
            transitionGroup.alpha = 0f;
            transitionGroup.interactable = false;
            transitionGroup.blocksRaycasts = true;
        }
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;

        if (transitionGroup != null)
        {
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            transitionGroup.alpha = progress * progress * (3f - 2f * progress);
        }

        if (spinner != null)
            spinner.Rotate(0f, 0f, rotationSpeed * Time.unscaledDeltaTime);

        if (spinnerSegments != null)
        {
            for (int i = 0; i < spinnerSegments.Length; i++)
            {
                Image segment = spinnerSegments[i];
                if (segment == null) continue;

                float phase = elapsed * pulseSpeed - (i * 0.42f);
                float glow = Mathf.InverseLerp(-1f, 1f, Mathf.Sin(phase));
                Color color = segment.color;
                color.a = Mathf.Lerp(0.28f, 1f, glow);
                segment.color = color;
            }
        }

        if (loadingLabel != null)
        {
            int dotCount = 1 + Mathf.FloorToInt(elapsed * 1.8f) % 3;
            loadingLabel.text = "Cargando" + new string('.', dotCount);
        }
    }
}
