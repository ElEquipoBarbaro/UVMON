using UnityEngine;

/// <summary>
/// Estado de batalla (runtime) de una extremidad. Analogo a CreatureRuntime pero para
/// una BodyPartDefinition. Ver Docs/CombatSystem/COMBAT_SYSTEM_SPEC.md sec 29-30.
/// </summary>
public class BodyPart
{
    public readonly BodyPartDefinition definition;

    public string IdParte => definition.idParte;
    public string NombreParte => definition.nombreParte;
    public int VidaMaxima { get; }
    public int VidaActual { get; private set; }
    public float PorcentajeAtaque { get; }
    public float PorcentajeAcertividad { get; }

    /// <summary>Se vuelve true una unica vez, al cruzar de vida positiva a 0.</summary>
    public bool EstadoDanado { get; private set; }

    /// <summary>false una vez que la extremidad llega a 0 de vida (ya no es un objetivo valido).</summary>
    public bool IsAlive => VidaActual > 0;

    public Sprite ReferenciaVisualNormal => definition.referenciaVisualNormal;
    public Sprite ReferenciaVisualDanada => definition.referenciaVisualDanada;

    /// <summary>Es la parte critica (porcentajeAtaque >= 100): un impacto exitoso derrota al enemigo.</summary>
    public bool EsParteCritica => PorcentajeAtaque >= 100f;

    public BodyPart(BodyPartDefinition definition)
    {
        this.definition = definition;

        VidaMaxima = Mathf.Max(0, definition.vidaMaxima);
        VidaActual = VidaMaxima;
        PorcentajeAtaque = Mathf.Clamp(definition.porcentajeAtaque, 0f, 100f);
        PorcentajeAcertividad = Mathf.Clamp(definition.porcentajeAcertividad, 0f, 100f);
        EstadoDanado = false;
    }

    /// <summary>
    /// Aplica danoFinal a la vida de la extremidad (una sola vez por llamada, sec 29).
    /// La vida nunca baja de 0. Devuelve true unicamente la primera vez que la parte
    /// cruza de vida positiva a 0 (sec 30) — el llamador debe usar ese valor para
    /// disparar el cambio visual/estado danado exactamente una vez.
    /// </summary>
    public bool ApplyDamage(int danoFinal)
    {
        int vidaAnterior = VidaActual;
        VidaActual = Mathf.Max(0, VidaActual - Mathf.Max(0, danoFinal));

        bool justCrossedToZero = vidaAnterior > 0 && VidaActual <= 0 && !EstadoDanado;

        if (justCrossedToZero)
            EstadoDanado = true;

        return justCrossedToZero;
    }
}
