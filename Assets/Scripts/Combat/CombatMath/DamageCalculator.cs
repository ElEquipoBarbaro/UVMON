using UnityEngine;

/// <summary>
/// Calculador de dano de ataque. Ver Docs/CombatSystem/COMBAT_SYSTEM_SPEC.md sec 24-28.
///
/// Orden obligatorio (ya asumiendo que el QTE se resolvio antes de llamar a este
/// calculador, como hace CombatManager.PlayerTurn): acertividad -> critico -> variacion -> dano.
/// La vida del proyecto usa int (CreatureRuntime.CurrentHP, BodyPart.VidaActual), por lo
/// que el resultado final se redondea con Mathf.RoundToInt (sec 26.1) de forma uniforme.
/// </summary>
public static class DamageCalculator
{
    public struct Result
    {
        /// <summary>El QTE previo al calculo fue exitoso.</summary>
        public bool qteExitoso;

        /// <summary>La comprobacion de acertividad paso (solo tiene sentido si qteExitoso).</summary>
        public bool ataqueImpacta;

        /// <summary>Ataque critico de derrota instantanea (sec 28).</summary>
        public bool esCritico;

        /// <summary>Valor generado una unica vez para la comprobacion de acertividad.</summary>
        public float valorImpacto;

        /// <summary>Variacion aleatoria (0.9 - 1.1) generada una unica vez; 0 si no aplica (fallo o critico).</summary>
        public float variacionAleatoria;

        /// <summary>Dano final sin redondear.</summary>
        public float danoFinal;

        /// <summary>Dano final redondeado al entero que usa el proyecto para la vida.</summary>
        public int danoFinalEntero;
    }

    /// <summary>
    /// Calcula el dano de un ataque contra una extremidad/objetivo.
    /// </summary>
    /// <param name="qteExitoso">Resultado del QTE de ataque, ya resuelto antes de llamar aqui.</param>
    /// <param name="danoBase">Dano base del ataque (p.ej. potencia del movimiento + ataque del atacante).</param>
    /// <param name="porcentajeAtaque">Porcentaje de ataque de la extremidad objetivo (0-100); >=100 habilita el critico.</param>
    /// <param name="porcentajeAcertividad">Porcentaje de acertividad de la extremidad objetivo (0-100).</param>
    /// <param name="vidaGlobalActualEnemigo">Vida global actual del enemigo, usada como dano si el golpe es critico.</param>
    /// <param name="random">Proveedor de aleatoriedad; si es null se usa UnityRandomProvider (produccion).</param>
    public static Result Calculate(
        bool qteExitoso,
        int danoBase,
        float porcentajeAtaque,
        float porcentajeAcertividad,
        int vidaGlobalActualEnemigo,
        IRandomProvider random = null
    )
    {
        IRandomProvider randomProvider = random ?? UnityRandomProvider.Instance;
        porcentajeAtaque = Mathf.Clamp(porcentajeAtaque, 0f, 100f);
        danoBase = Mathf.Max(0, danoBase);

        Result result = new Result
        {
            qteExitoso = qteExitoso,
            ataqueImpacta = false,
            esCritico = false,
            valorImpacto = 0f,
            variacionAleatoria = 0f,
            danoFinal = 0f,
            danoFinalEntero = 0
        };

        // 1-5. QTE fallido: dano 0, no se genera acertividad ni variacion.
        if (!qteExitoso)
            return result;

        // 6-8. Acertividad (unica generacion de valorImpacto por ataque).
        AccuracyChecker.Result accuracy = AccuracyChecker.Evaluate(qteExitoso, porcentajeAcertividad, randomProvider);
        result.ataqueImpacta = accuracy.ataqueImpacta;
        result.valorImpacto = accuracy.valorImpacto;

        if (!accuracy.ataqueImpacta)
            return result;

        // 9-10. Critico: requiere QTE + impacto exitosos y porcentajeAtaque >= 100.
        bool esCritico = porcentajeAtaque >= 100f;
        result.esCritico = esCritico;

        if (esCritico)
        {
            int vidaGlobal = Mathf.Max(0, vidaGlobalActualEnemigo);
            result.danoFinal = vidaGlobal;
            result.danoFinalEntero = vidaGlobal;
            return result;
        }

        // 11-12. Variacion aleatoria (unica generacion por ataque normal) + formula de dano.
        float variacion = randomProvider.Range(0.9f, 1.1f);
        result.variacionAleatoria = variacion;

        float pAtaque = porcentajeAtaque / 100f;
        float pAcertividad = Mathf.Clamp(porcentajeAcertividad, 0f, 100f) / 100f;

        float danoFinal = Mathf.Max(0f, danoBase * pAtaque * pAcertividad * variacion);
        result.danoFinal = danoFinal;
        result.danoFinalEntero = Mathf.Max(0, Mathf.RoundToInt(danoFinal));

        return result;
    }
}
