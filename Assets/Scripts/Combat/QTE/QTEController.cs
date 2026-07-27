using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QTEController : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private RectTransform ringsContainer;
    [SerializeField] private RectTransform shrinkingRing;
    [SerializeField] private RectTransform targetRing;
    [SerializeField] private Image shrinkingRingImage;
    [SerializeField] private Image targetRingImage;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Colores")]
    [SerializeField] private Color idleColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color successZoneColor = new Color(0.3f, 0.95f, 0.45f);
    [SerializeField] private Color failColor = new Color(0.95f, 0.25f, 0.25f);

    private const float ToleranceRatio = 0.12f;
    private const float MinTolerance = 6f;

    public IEnumerator RunQTE(QTEData data, Action<bool> onComplete)
    {
        if (data == null)
        {
            onComplete?.Invoke(true);
            yield break;
        }

        float tolerance = Mathf.Max(MinTolerance, (data.outerRadius - data.innerRadius) * ToleranceRatio);
        float currentRadius = data.outerRadius;
        bool? result = null;

        if (ringsContainer != null)
            ringsContainer.anchoredPosition = data.position;

        if (targetRing != null)
            targetRing.sizeDelta = Vector2.one * data.innerRadius * 2f;

        if (shrinkingRingImage != null)
            shrinkingRingImage.color = idleColor;

        if (resultText != null)
            resultText.text = string.Empty;

        if (overlayRoot != null)
            overlayRoot.SetActive(true);

        while (result == null)
        {
            currentRadius -= data.shrinkSpeed * Time.deltaTime;

            if (shrinkingRing != null)
                shrinkingRing.sizeDelta = Vector2.one * Mathf.Max(0f, currentRadius) * 2f;

            bool inZone = Mathf.Abs(currentRadius - data.innerRadius) <= tolerance;

            if (shrinkingRingImage != null)
                shrinkingRingImage.color = inZone ? successZoneColor : idleColor;

            if (Input.GetMouseButtonDown(0))
            {
                result = inZone;
            }
            else if (currentRadius <= data.innerRadius - tolerance)
            {
                result = false;
            }

            yield return null;
        }

        if (shrinkingRingImage != null)
            shrinkingRingImage.color = result.Value ? successZoneColor : failColor;

        if (resultText != null)
            resultText.text = result.Value ? "¡EXITO!" : "¡FALLO!";

        yield return new WaitForSeconds(0.5f);

        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        onComplete?.Invoke(result.Value);
    }
}
