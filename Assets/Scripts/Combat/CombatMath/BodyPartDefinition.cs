using UnityEngine;

/// <summary>
/// Datos de diseno de una extremidad/parte atacable de una criatura (spec sec 2).
/// Se autora directamente en el Inspector de CreatureData.bodyParts.
/// </summary>
[System.Serializable]
public class BodyPartDefinition
{
    [Tooltip("Identificador estable de la parte (p.ej. \"head\", \"body\"). No se usa para mostrar texto.")]
    public string idParte;

    [Tooltip("Nombre visible de la parte (p.ej. \"Cabeza\").")]
    public string nombreParte;

    [Tooltip("Vida maxima de esta extremidad.")]
    public int vidaMaxima = 50;

    [Tooltip("Porcentaje de ataque efectivo contra esta parte (0-100). >=100 habilita el golpe critico de derrota instantanea.")]
    [Range(0f, 100f)]
    public float porcentajeAtaque = 60f;

    [Tooltip("Porcentaje de acertividad al apuntar a esta parte (0-100). Partes criticas conviene que sea bajo, para balancear.")]
    [Range(0f, 100f)]
    public float porcentajeAcertividad = 80f;

    [Header("Visual")]
    [Tooltip("Sprite mostrado mientras la parte no esta danada.")]
    public Sprite referenciaVisualNormal;

    [Tooltip("Sprite mostrado una vez que la parte llega a 0 de vida. Puede quedar sin asignar si no hay variante danada.")]
    public Sprite referenciaVisualDanada;

    [Header("Posicion en pantalla")]
    [Tooltip("Posicion (anchoredPosition) del sprite de esta parte respecto al contenedor del enemigo. Las partes de un mismo enemigo suelen compartir el mismo lienzo/posicion para quedar superpuestas.")]
    public Vector2 anchoredPosition = Vector2.zero;
}
