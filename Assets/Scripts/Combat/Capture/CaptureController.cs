using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CaptureController : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private RectTransform captureAreaRect;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Indicador")]
    [SerializeField] private RectTransform indicatorRect;
    [SerializeField] private Image indicatorImage;

    [Header("Frasco")]
    [SerializeField] private RectTransform jarRect;
    [SerializeField] private Image jarImage;

    [Header("Colores")]
    [SerializeField] private Color idleColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color successColor = new Color(0.3f, 0.95f, 0.45f);
    [SerializeField] private Color failColor = new Color(0.95f, 0.25f, 0.25f);

    private enum CaptureState
    {
        Inactive,
        CheckingInventory,
        Preparing,
        Active,
        Dropping,
        Resolving,
        Success,
        Failure,
        Closing
    }

    private CaptureState state = CaptureState.Inactive;
    private bool isRunning = false;

    private Vector2 posicionIndicador;
    private Vector2 posicionDestino;
    private float radioActual;
    private float velocidadReduccion;

    /// <summary>
    /// Corre el desafio de captura completo: valida el inventario, mueve/reduce el
    /// circulo indicador dentro del area circular delimitada, deja caer el frasco
    /// (que se mueve como un riel sobre esa misma circunferencia) al hacer clic, y
    /// valida el impacto por geometria. No debe llamarse mientras ya hay un intento
    /// en curso.
    /// </summary>
    public IEnumerator RunCapture(CreatureData targetCreature, InventorySO inventory, CaptureData data, Action<CaptureResult> onComplete)
    {
        if (isRunning)
        {
            onComplete?.Invoke(CaptureResult.Fail(CaptureFailReason.AlreadyResolved));
            yield break;
        }

        if (targetCreature == null)
        {
            onComplete?.Invoke(CaptureResult.Fail(CaptureFailReason.InvalidTarget));
            yield break;
        }

        if (data == null || captureAreaRect == null || indicatorRect == null || jarRect == null)
        {
            onComplete?.Invoke(CaptureResult.Fail(CaptureFailReason.InvalidConfiguration));
            yield break;
        }

        isRunning = true;

        CaptureResult result = default;
        yield return RunCaptureInternal(targetCreature, inventory, data, r => result = r);

        isRunning = false;
        onComplete?.Invoke(result);
    }

    private IEnumerator RunCaptureInternal(CreatureData targetCreature, InventorySO inventory, CaptureData data, Action<CaptureResult> onComplete)
    {
        state = CaptureState.CheckingInventory;

        if (inventory == null || inventory.GetCaptureJarCount() <= 0)
        {
            state = CaptureState.Failure;
            onComplete?.Invoke(CaptureResult.Fail(CaptureFailReason.NoJar));
            yield break;
        }

        state = CaptureState.Preparing;

        float pCaptura = Mathf.Clamp01(targetCreature.porcentajeCaptura / 100f);
        float radioInicial = Mathf.Lerp(data.radioInicialMinimo, data.radioInicialMaximo, pCaptura);
        velocidadReduccion = Mathf.Lerp(data.velocidadReduccionMaxima, data.velocidadReduccionMinima, pCaptura);
        radioActual = radioInicial;

        Rect rect = captureAreaRect.rect;
        float areaRadius = Mathf.Min(rect.width, rect.height) * 0.5f;

        if (!TryGetMaxDestinoRadius(areaRadius, radioActual, data.margenArea, out float maxDestinoRadius))
        {
            // Area demasiado chica para el radio inicial: recortamos el radio al maximo que entra.
            float radioMaximoPorArea = areaRadius - data.margenArea;

            if (radioMaximoPorArea < data.radioMinimoPermitido ||
                !TryGetMaxDestinoRadius(areaRadius, radioMaximoPorArea, data.margenArea, out maxDestinoRadius))
            {
                state = CaptureState.Failure;
                onComplete?.Invoke(CaptureResult.Fail(CaptureFailReason.InvalidConfiguration));
                yield break;
            }

            radioActual = radioMaximoPorArea;
        }

        posicionIndicador = Vector2.zero;
        posicionDestino = GenerateDestino(areaRadius, radioActual, data, posicionIndicador);

        if (overlayRoot != null)
            overlayRoot.SetActive(true);

        if (resultText != null)
            resultText.text = string.Empty;

        ApplyIndicatorVisual(idleColor);

        jarRect.anchoredPosition = new Vector2(areaRadius, 0f);

        state = CaptureState.Active;

        float tiempoTranscurrido = 0f;
        bool jarConsumed = false;
        CaptureFailReason activeFailReason = CaptureFailReason.None;
        bool clicked = false;

        while (state == CaptureState.Active)
        {
            float deltaTime = Time.deltaTime;
            tiempoTranscurrido += deltaTime;

            UpdateIndicator(areaRadius, data, deltaTime);
            UpdateJarRail(areaRadius);

            if (Input.GetMouseButtonDown(0))
            {
                clicked = true;
                break;
            }

            if (radioActual <= data.radioMinimoPermitido)
            {
                activeFailReason = CaptureFailReason.IndicatorTooSmall;
                break;
            }

            if (tiempoTranscurrido >= data.tiempoMaximoCaptura)
            {
                activeFailReason = CaptureFailReason.Timeout;
                break;
            }

            yield return null;
        }

        if (!clicked)
        {
            state = CaptureState.Failure;
            yield return ShowResult(false);
            onComplete?.Invoke(CaptureResult.Fail(activeFailReason, jarConsumed: false));
            yield break;
        }

        if (!inventory.TryConsumeCaptureJar())
        {
            state = CaptureState.Failure;
            yield return ShowResult(false);
            onComplete?.Invoke(CaptureResult.Fail(CaptureFailReason.NoJar));
            yield break;
        }

        jarConsumed = true;
        state = CaptureState.Dropping;

        // El frasco viaja desde su punto fijo en el riel (borde de la circunferencia)
        // hacia el centro del area; no puede corregirse durante el trayecto.
        Vector2 posicionInicioCaida = jarRect.anchoredPosition;
        Vector2 posicionFinCaida = Vector2.zero;

        float caidaTranscurrida = 0f;
        Vector2 posicionImpactoFrasco = posicionFinCaida;
        float distanciaImpacto = 0f;
        bool capturaExitosa = false;

        while (caidaTranscurrida < data.duracionCaidaFrasco)
        {
            float deltaTime = Time.deltaTime;
            caidaTranscurrida += deltaTime;

            UpdateIndicator(areaRadius, data, deltaTime);

            float u = Mathf.Clamp01(caidaTranscurrida / data.duracionCaidaFrasco);
            Vector2 posicionFrasco = Vector2.Lerp(posicionInicioCaida, posicionFinCaida, u);
            jarRect.anchoredPosition = posicionFrasco;

            // El impacto se resuelve apenas el frasco, en su trayecto de caida,
            // entra en contacto con el circulo (no solo si llega al centro).
            float distanciaActual = Vector2.Distance(posicionFrasco, posicionIndicador);

            if (distanciaActual <= radioActual + data.toleranciaImpacto)
            {
                posicionImpactoFrasco = posicionFrasco;
                distanciaImpacto = distanciaActual;
                capturaExitosa = true;
                break;
            }

            yield return null;
        }

        if (!capturaExitosa)
        {
            jarRect.anchoredPosition = posicionFinCaida;
            posicionImpactoFrasco = posicionFinCaida;
            distanciaImpacto = Vector2.Distance(posicionImpactoFrasco, posicionIndicador);
            capturaExitosa = distanciaImpacto <= radioActual + data.toleranciaImpacto;
        }

        state = CaptureState.Resolving;

        CaptureResult finalResult = new CaptureResult
        {
            success = capturaExitosa,
            failureReason = capturaExitosa ? CaptureFailReason.None : CaptureFailReason.MissedIndicator,
            capturedUVGmon = capturaExitosa ? targetCreature : null,
            jarConsumed = jarConsumed,
            impactDistance = distanciaImpacto,
            indicatorRadiusAtImpact = radioActual
        };

        state = capturaExitosa ? CaptureState.Success : CaptureState.Failure;
        yield return ShowResult(capturaExitosa);

        onComplete?.Invoke(finalResult);
    }

    private void UpdateIndicator(float areaRadius, CaptureData data, float deltaTime)
    {
        radioActual = Mathf.Max(data.radioMinimoPermitido, radioActual - velocidadReduccion * deltaTime);

        posicionIndicador = Vector2.MoveTowards(posicionIndicador, posicionDestino, data.velocidadMovimientoIndicador * deltaTime);

        if (Vector2.Distance(posicionIndicador, posicionDestino) <= data.epsilonDestino)
            posicionDestino = GenerateDestino(areaRadius, radioActual, data, posicionIndicador);

        ApplyIndicatorVisual(idleColor);
    }

    private void ApplyIndicatorVisual(Color color)
    {
        if (indicatorRect != null)
        {
            indicatorRect.anchoredPosition = posicionIndicador;
            indicatorRect.sizeDelta = Vector2.one * (radioActual * 2f);
        }

        if (indicatorImage != null)
            indicatorImage.color = color;
    }

    /// <summary>Mueve el frasco como si estuviera sobre un riel: fijo a la circunferencia del area, siguiendo el angulo del cursor.</summary>
    private void UpdateJarRail(float areaRadius)
    {
        if (jarRect == null || jarRect.parent == null)
            return;

        RectTransform parentRect = jarRect.parent as RectTransform;

        if (parentRect == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, null, out Vector2 localPoint))
            return;

        float angle = Mathf.Atan2(localPoint.y, localPoint.x);
        jarRect.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * areaRadius;
    }

    private IEnumerator ShowResult(bool success)
    {
        Color finalColor = success ? successColor : failColor;

        if (indicatorImage != null)
            indicatorImage.color = finalColor;

        if (resultText != null)
            resultText.text = success ? "¡CAPTURADO!" : "¡ESCAPO!";

        yield return new WaitForSeconds(0.9f);

        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        state = CaptureState.Inactive;
    }

    /// <summary>Radio maximo (desde el centro) en el que puede caer el destino del indicador, dado el radio actual y el margen del area.</summary>
    private static bool TryGetMaxDestinoRadius(float areaRadius, float radio, float margenArea, out float maxDestinoRadius)
    {
        maxDestinoRadius = areaRadius - radio - margenArea;
        return maxDestinoRadius >= 0f;
    }

    private static Vector2 GenerateDestino(float areaRadius, float radio, CaptureData data, Vector2 desdePosicion)
    {
        if (!TryGetMaxDestinoRadius(areaRadius, radio, data.margenArea, out float maxDestinoRadius))
            return Vector2.zero;

        for (int attempt = 0; attempt < data.maxIntentosDestino; attempt++)
        {
            Vector2 candidate = UnityEngine.Random.insideUnitCircle * maxDestinoRadius;

            if (Vector2.Distance(candidate, desdePosicion) >= data.distanciaMinimaEntreDestinos)
                return candidate;
        }

        return UnityEngine.Random.insideUnitCircle * maxDestinoRadius;
    }
}
