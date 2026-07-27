using System;
using System.Collections;
using System.Collections.Generic;
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
    private const float BetweenStepsDelay = 0.15f;
    private const float OverlapPadding = 24f;
    private const int OverlapMaxAttempts = 30;

    private class ParallelCircle
    {
        public QTEData data;
        public RectTransform container;
        public RectTransform ring;
        public Image ringImage;
        public float currentRadius;
        public float tolerance;
        public bool resolved;
    }

    /// <summary>Calcula donde debe aparecer un circulo: su posicion fija, o una al azar dentro de la pantalla de batalla.</summary>
    private Vector2 GetSpawnPosition(QTEData data)
    {
        if (!data.randomizePosition || overlayRoot == null)
            return data.position;

        RectTransform overlayRT = overlayRoot.transform as RectTransform;
        if (overlayRT == null)
            return data.position;

        Rect rect = overlayRT.rect;
        float margin = data.outerRadius + 10f;
        float halfW = Mathf.Max(0f, rect.width / 2f - margin);
        float halfH = Mathf.Max(0f, rect.height / 2f - margin);

        return new Vector2(UnityEngine.Random.Range(-halfW, halfW), UnityEngine.Random.Range(-halfH, halfH));
    }

    /// <summary>
    /// Como GetSpawnPosition, pero si la posicion es al azar intenta varias veces
    /// evitar que el circulo quede superpuesto con los que ya se colocaron (solo
    /// aplica cuando 'randomizePosition' esta activo; una posicion fija se respeta tal cual).
    /// </summary>
    private Vector2 GetSpawnPositionAvoidingOverlap(QTEData data, List<ParallelCircle> placed)
    {
        if (!data.randomizePosition || placed.Count == 0)
            return GetSpawnPosition(data);

        Vector2 best = GetSpawnPosition(data);
        float bestMinGap = MinGapToOthers(best, data.outerRadius, placed);

        if (bestMinGap >= 0f)
            return best;

        for (int attempt = 1; attempt < OverlapMaxAttempts; attempt++)
        {
            Vector2 candidate = GetSpawnPosition(data);
            float gap = MinGapToOthers(candidate, data.outerRadius, placed);

            if (gap >= 0f)
                return candidate;

            if (gap > bestMinGap)
            {
                bestMinGap = gap;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>Distancia libre minima entre un candidato y los circulos ya colocados (negativo = se solapan).</summary>
    private float MinGapToOthers(Vector2 candidate, float candidateRadius, List<ParallelCircle> placed)
    {
        float minGap = float.MaxValue;

        foreach (ParallelCircle p in placed)
        {
            if (p.container == null)
                continue;

            float requiredDist = candidateRadius + p.data.outerRadius + OverlapPadding;
            float actualDist = Vector2.Distance(candidate, p.container.anchoredPosition);
            minGap = Mathf.Min(minGap, actualDist - requiredDist);
        }

        return minGap;
    }

    /// <summary>
    /// La zona de acierto es SOLO antes de llegar al radio objetivo: desde
    /// innerRadius+tolerance (todavia grande) hasta innerRadius (justo al llegar).
    /// Una vez que el radio queda por debajo de innerRadius sin que le hayan
    /// hecho clic, ya no cuenta como acierto (hay que reaccionar antes de que
    /// el circulo llegue, no despues).
    /// </summary>
    private static bool IsInZone(float currentRadius, float innerRadius, float tolerance)
    {
        return currentRadius >= innerRadius && currentRadius <= innerRadius + tolerance;
    }

    /// <summary>Corre un unico circulo. Requiere que el overlay ya este activo.</summary>
    private IEnumerator RunCircle(QTEData data, Action<bool> onResult)
    {
        float tolerance = Mathf.Max(MinTolerance, (data.outerRadius - data.innerRadius) * ToleranceRatio);
        float currentRadius = data.outerRadius;
        bool? result = null;

        if (ringsContainer != null)
            ringsContainer.anchoredPosition = GetSpawnPosition(data);

        if (targetRing != null)
            targetRing.sizeDelta = Vector2.one * data.innerRadius * 2f;

        if (shrinkingRingImage != null)
            shrinkingRingImage.color = idleColor;

        if (resultText != null)
            resultText.text = string.Empty;

        while (result == null)
        {
            currentRadius -= data.shrinkSpeed * Time.deltaTime;

            if (shrinkingRing != null)
                shrinkingRing.sizeDelta = Vector2.one * Mathf.Max(0f, currentRadius) * 2f;

            bool inZone = IsInZone(currentRadius, data.innerRadius, tolerance);

            if (shrinkingRingImage != null)
                shrinkingRingImage.color = inZone ? successZoneColor : idleColor;

            if (Input.GetMouseButtonDown(0))
            {
                // Exito: toco el circulo antes de que llegue al radio interno.
                result = currentRadius >= data.innerRadius;
            }
            else if (currentRadius < data.innerRadius)
            {
                result = false;
            }

            yield return null;
        }

        if (shrinkingRingImage != null)
            shrinkingRingImage.color = result.Value ? successZoneColor : failColor;

        if (resultText != null)
            resultText.text = result.Value ? "¡EXITO!" : "¡FALLO!";

        onResult?.Invoke(result.Value);
    }

    /// <summary>Corre un unico QTE. Equivale a una cadena de un solo paso.</summary>
    public IEnumerator RunQTE(QTEData data, Action<bool> onComplete)
    {
        if (data == null)
        {
            onComplete?.Invoke(true);
            yield break;
        }

        yield return RunQTEChain(new[] { data }, onComplete);
    }

    /// <summary>
    /// Corre varios QTE en secuencia (combo). Hay que acertar todos, en orden,
    /// para que el resultado final sea exito; el primer fallo corta la cadena.
    /// </summary>
    public IEnumerator RunQTEChain(IReadOnlyList<QTEData> chain, Action<bool> onComplete)
    {
        if (chain == null || chain.Count == 0)
        {
            onComplete?.Invoke(true);
            yield break;
        }

        if (overlayRoot != null)
            overlayRoot.SetActive(true);

        bool overallSuccess = true;

        for (int i = 0; i < chain.Count; i++)
        {
            if (chain[i] == null)
                continue;

            bool stepResult = false;
            yield return RunCircle(chain[i], r => stepResult = r);

            if (!stepResult)
            {
                overallSuccess = false;
                break;
            }

            if (i < chain.Count - 1)
                yield return new WaitForSeconds(BetweenStepsDelay);
        }

        yield return new WaitForSeconds(0.5f);

        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        onComplete?.Invoke(overallSuccess);
    }

    /// <summary>
    /// Corre varios QTE al mismo tiempo, cada uno en su propia posicion. Hay que
    /// acertarlos todos (clic cerca de cada uno, en cualquier orden); si fallas
    /// cualquiera (clic fuera de zona en el circulo mas cercano, o un circulo se
    /// reduce de mas sin que le hagas clic), el ataque completo falla.
    /// </summary>
    public IEnumerator RunQTEParallel(IReadOnlyList<QTEData> circles, Action<bool> onComplete)
    {
        List<QTEData> validCircles = new List<QTEData>();
        if (circles != null)
        {
            foreach (QTEData d in circles)
            {
                if (d != null)
                    validCircles.Add(d);
            }
        }

        if (validCircles.Count == 0)
        {
            onComplete?.Invoke(true);
            yield break;
        }

        if (validCircles.Count == 1 || ringsContainer == null)
        {
            yield return RunQTEChain(validCircles, onComplete);
            yield break;
        }

        if (overlayRoot != null)
            overlayRoot.SetActive(true);

        ringsContainer.gameObject.SetActive(false);

        if (resultText != null)
            resultText.text = string.Empty;

        List<ParallelCircle> active = new List<ParallelCircle>();
        List<GameObject> spawned = new List<GameObject>();

        foreach (QTEData data in validCircles)
        {
            GameObject instance = Instantiate(ringsContainer.gameObject, ringsContainer.parent);
            instance.SetActive(true);
            spawned.Add(instance);

            RectTransform containerRT = instance.GetComponent<RectTransform>();
            containerRT.anchoredPosition = GetSpawnPositionAvoidingOverlap(data, active);

            RectTransform targetRT = instance.transform.Find("TargetRing") as RectTransform;
            RectTransform ringRT = instance.transform.Find("ShrinkingRing") as RectTransform;
            Image ringImg = ringRT != null ? ringRT.GetComponent<Image>() : null;

            if (targetRT != null)
                targetRT.sizeDelta = Vector2.one * data.innerRadius * 2f;

            if (ringImg != null)
                ringImg.color = idleColor;

            active.Add(new ParallelCircle
            {
                data = data,
                container = containerRT,
                ring = ringRT,
                ringImage = ringImg,
                currentRadius = data.outerRadius,
                tolerance = Mathf.Max(MinTolerance, (data.outerRadius - data.innerRadius) * ToleranceRatio),
                resolved = false
            });
        }

        bool? overallResult = null;

        while (overallResult == null)
        {
            bool anyPending = false;

            foreach (ParallelCircle c in active)
            {
                if (c.resolved)
                    continue;

                anyPending = true;
                c.currentRadius -= c.data.shrinkSpeed * Time.deltaTime;

                if (c.ring != null)
                    c.ring.sizeDelta = Vector2.one * Mathf.Max(0f, c.currentRadius) * 2f;

                bool inZone = IsInZone(c.currentRadius, c.data.innerRadius, c.tolerance);

                if (c.ringImage != null)
                    c.ringImage.color = inZone ? successZoneColor : idleColor;

                if (c.currentRadius < c.data.innerRadius)
                {
                    overallResult = false;
                    break;
                }
            }

            if (overallResult == null && Input.GetMouseButtonDown(0))
            {
                ParallelCircle nearest = null;
                float nearestDist = float.MaxValue;

                foreach (ParallelCircle c in active)
                {
                    if (c.resolved || c.container == null)
                        continue;

                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, c.container.position);
                    float dist = Vector2.Distance(screenPos, (Vector2)Input.mousePosition);

                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = c;
                    }
                }

                if (nearest != null)
                {
                    // Exito: toco ese circulo antes de que llegue a su radio interno.
                    bool reachedTarget = nearest.currentRadius >= nearest.data.innerRadius;

                    if (reachedTarget)
                        nearest.resolved = true;
                    else
                        overallResult = false;
                }
            }

            if (overallResult == null && !anyPending)
                overallResult = true;

            yield return null;
        }

        Color finalColor = overallResult.Value ? successZoneColor : failColor;
        foreach (ParallelCircle c in active)
        {
            if (c.ringImage != null)
                c.ringImage.color = finalColor;
        }

        if (resultText != null)
            resultText.text = overallResult.Value ? "¡EXITO!" : "¡FALLO!";

        yield return new WaitForSeconds(0.5f);

        foreach (GameObject go in spawned)
            Destroy(go);

        ringsContainer.gameObject.SetActive(true);

        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        onComplete?.Invoke(overallResult.Value);
    }
}
