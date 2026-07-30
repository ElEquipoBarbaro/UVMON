using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Capture/Capture Data", fileName = "NewCaptureData")]
public class CaptureData : ScriptableObject
{
    [Header("Radio inicial del circulo indicador")]
    public float radioInicialMinimo = 40f;
    public float radioInicialMaximo = 100f;

    [Header("Velocidad de reduccion del circulo")]
    public float velocidadReduccionMinima = 10f;
    public float velocidadReduccionMaxima = 50f;

    [Tooltip("El circulo deja de encogerse al llegar a este radio.")]
    public float radioMinimoPermitido = 15f;

    [Header("Movimiento del indicador")]
    public float velocidadMovimientoIndicador = 140f;
    public float epsilonDestino = 4f;
    public float distanciaMinimaEntreDestinos = 20f;
    public int maxIntentosDestino = 10;

    [Tooltip("Margen entre el borde del area de captura y el borde del circulo indicador.")]
    public float margenArea = 10f;

    [Header("Caida del frasco")]
    public float duracionCaidaFrasco = 0.5f;

    [Header("Tiempo")]
    [Tooltip("Tiempo maximo (segundos) para hacer clic antes de que la captura falle por Timeout.")]
    public float tiempoMaximoCaptura = 8f;

    [Header("Validacion de impacto")]
    public float toleranciaImpacto = 4f;
}
