using UnityEngine;

/// <summary>
/// Comprobacion de acertividad. Ver Docs/CombatSystem/COMBAT_SYSTEM_SPEC.md sec 23.
/// Se evalua unicamente si el QTE fue exitoso; valorImpacto se genera una unica vez por ataque.
/// </summary>
public static class AccuracyChecker
{
    public struct Result
    {
        public bool ataqueImpacta;
        public float valorImpacto;
        public float porcentajeAcertividad;
    }

    /// <summary>
    /// Evalua si el ataque impacta. Si qteExitoso es false, no se genera valorImpacto
    /// (no tiene sentido comprobar acertividad de un QTE fallido) y el resultado es fallo.
    /// </summary>
    public static Result Evaluate(bool qteExitoso, float porcentajeAcertividad, IRandomProvider random)
    {
        porcentajeAcertividad = Mathf.Clamp(porcentajeAcertividad, 0f, 100f);

        if (!qteExitoso)
        {
            return new Result
            {
                ataqueImpacta = false,
                valorImpacto = 0f,
                porcentajeAcertividad = porcentajeAcertividad
            };
        }

        IRandomProvider randomProvider = random ?? UnityRandomProvider.Instance;
        float valorImpacto = randomProvider.Range(0f, 100f);

        return new Result
        {
            ataqueImpacta = valorImpacto <= porcentajeAcertividad,
            valorImpacto = valorImpacto,
            porcentajeAcertividad = porcentajeAcertividad
        };
    }
}
