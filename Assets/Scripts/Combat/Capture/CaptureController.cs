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
    /// circulo indicador, deja caer el frasco al hacer clic y valida el impacto por
    /// geometria. No debe llamarse mientras ya hay un intento en curso.
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

        Rect bounds = captureAreaRect.rect;

        if (!TryGetValidDestinoBounds(bounds, radioActual, data.margenArea, out Rect destinoBounds))
        {
            // Area demasiado chica para el radio inicial: recortamos el radio al maximo que entra.
            float radioMaximoPorArea = Mathf.Min(bounds.width, bounds.height) / 2f - data.margenArea;

            if (radioMaximoPorArea < data.radioMinimoPermitido ||
                !TryGetValidDestinoBounds(bounds, radioMaximoPorArea, data.margenArea, out destinoBounds))
            {
                state = CaptureState.Failure;
                onComplete?.Invoke(CaptureResult.Fail(CaptureFailReason.InvalidConfiguration));
                yield break;
            }

            radioActual = radioMaximoPorArea;
        }

        posicionIndicador = Vector2.zero;
        posicionDestino = GenerateDestino(bounds, radioActual, data, posicionIndicador);

        if (overlayRoot != null)
            overlayRoot.SetActive(true);

        if (resultText != null)
            resultText.text = string.Empty;

        ApplyIndicatorVisual(idleColor);

        float jarHalfWidth = jarRect.rect.width * 0.5f;
        float jarHeight = jarRect.rect.height;
        float jarStartY = bounds.yMax - data.margenArea - jarHeight;

        jarRect.anchoredPosition = new Vector2(0f, jarStartY);

        state = CaptureState.Active;

        float tiempoTranscurrido = 0f;
        bool jarConsumed = false;
        CaptureFailReason activeFailReason = CaptureFailReason.None;
        bool clicked = false;

        while (state == CaptureState.Active)
        {
            float deltaTime = Time.deltaTime;
            tiempoTranscurrido += deltaTime;

            UpdateIndicator(bounds, data, deltaTime);
            UpdateJarFollow(bounds, jarHalfWidth, jarStartY);

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

        float lockedX = Mathf.Clamp(jarRect.anchoredPosition.x, bounds.xMin + jarHalfWidth, bounds.xMax - jarHalfWidth);
        Vector2 posicionInicioCaida = new Vector2(lockedX, jarStartY);
        Vector2 posicionFinCaida = new Vector2(lockedX, bounds.yMin);

        float caidaTranscurrida = 0f;
        Vector2 posicionImpactoFrasco = posicionFinCaida;
        float distanciaImpacto = 0f;
        bool capturaExitosa = false;

        while (caidaTranscurrida < data.duracionCaidaFrasco)
        {
            float deltaTime = Time.deltaTime;
            caidaTranscurrida += deltaTime;

            UpdateIndicator(bounds, data, deltaTime);

            float u = Mathf.Clamp01(caidaTranscurrida / data.duracionCaidaFrasco);
            Vector2 posicionFrasco = Vector2.Lerp(posicionInicioCaida, posicionFinCaida, u);
            jarRect.anchoredPosition = posicionFrasco;

            // El impacto se resuelve apenas el frasco, en su trayecto de caida,
            // entra en contacto con el circulo (no solo si llega hasta el piso).
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

    private void UpdateIndicator(Rect bounds, CaptureData data, float deltaTime)
    {
        radioActual = Mathf.Max(data.radioMinimoPermitido, radioActual - velocidadReduccion * deltaTime);

        posicionIndicador = Vector2.MoveTowards(posicionIndicador, posicionDestino, data.velocidadMovimientoIndicador * deltaTime);

        if (Vector2.Distance(posicionIndicador, posicionDestino) <= data.epsilonDestino)
            posicionDestino = GenerateDestino(bounds, radioActual, data, posicionIndicador);

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

    private void UpdateJarFollow(Rect bounds, float jarHalfWidth, float jarStartY)
    {
        if (jarRect == null || jarRect.parent == null)
            return;

        RectTransform parentRect = jarRect.parent as RectTransform;

        if (parentRect == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, null, out Vector2 localPoint))
            return;

        float clampedX = Mathf.Clamp(localPoint.x, bounds.xMin + jarHalfWidth, bounds.xMax - jarHalfWidth);
        jarRect.anchoredPosition = new Vector2(clampedX, jarStartY);
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

    /// <summary>Rango valido para el centro del indicador dado el radio y el margen del area.</summary>
    private static bool TryGetValidDestinoBounds(Rect bounds, float radio, float margenArea, out Rect destinoBounds)
    {
        float xMin = bounds.xMin + radio + margenArea;
        float xMax = bounds.xMax - radio - margenArea;
        float yMin = bounds.yMin + radio + margenArea;
        float yMax = bounds.yMax - radio - margenArea;

        destinoBounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

        return xMin <= xMax && yMin <= yMax;
    }

    private static Vector2 GenerateDestino(Rect bounds, float radio, CaptureData data, Vector2 desdePosicion)
    {
        if (!TryGetValidDestinoBounds(bounds, radio, data.margenArea, out Rect destinoBounds))
            return Vector2.zero;

        for (int attempt = 0; attempt < data.maxIntentosDestino; attempt++)
        {
            Vector2 candidate = new Vector2(
                UnityEngine.Random.Range(destinoBounds.xMin, destinoBounds.xMax),
                UnityEngine.Random.Range(destinoBounds.yMin, destinoBounds.yMax)
            );

            if (Vector2.Distance(candidate, desdePosicion) >= data.distanciaMinimaEntreDestinos)
                return candidate;
        }

        return new Vector2(
            UnityEngine.Random.Range(destinoBounds.xMin, destinoBounds.xMax),
            UnityEngine.Random.Range(destinoBounds.yMin, destinoBounds.yMax)
        );
    }
}
