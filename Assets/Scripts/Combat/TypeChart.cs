using UnityEngine;

public static class TypeChart
{
    public static float GetMultiplier(CreatureType attack, CreatureType defense)
    {
        switch (attack)
        {
            case CreatureType.Digital:
                switch (defense)
                {
                    case CreatureType.Entropia: return 0.5f;
                    case CreatureType.Organico: return 0.5f;
                    case CreatureType.Estrategia: return 2f;
                    case CreatureType.Mente: return 0.5f;
                    case CreatureType.Nexo: return 2f;
                    default: return 1f;
                }

            case CreatureType.Mecanico:
                switch (defense)
                {
                    case CreatureType.Entropia: return 0.5f;
                    case CreatureType.Organico: return 0.5f;
                    case CreatureType.Fosil: return 2f;
                    case CreatureType.Estructura: return 2f;
                    default: return 1f;
                }

            case CreatureType.Armonia:
                switch (defense)
                {
                    case CreatureType.Entropia: return 2f;
                    case CreatureType.Organico: return 0.5f;
                    case CreatureType.Vector: return 0.5f;
                    case CreatureType.Mente: return 2f;
                    case CreatureType.Nexo: return 0.5f;
                    default: return 1f;
                }

            case CreatureType.Entropia:
                switch (defense)
                {
                    case CreatureType.Digital: return 2f;
                    case CreatureType.Mecanico: return 2f;
                    case CreatureType.Armonia: return 2f;
                    case CreatureType.Entropia: return 0.5f;
                    case CreatureType.Fosil: return 2f;
                    case CreatureType.Estructura: return 2f;
                    case CreatureType.Nexo: return 2f;
                    case CreatureType.Conducto: return 2f;
                    default: return 1f;
                }

            case CreatureType.Organico:
                switch (defense)
                {
                    case CreatureType.Digital: return 2f;
                    case CreatureType.Mecanico: return 2f;
                    case CreatureType.Organico: return 0.5f;
                    case CreatureType.Fosil: return 0.5f;
                    case CreatureType.Estrategia: return 0.5f;
                    default: return 1f;
                }

            case CreatureType.Fosil:
                switch (defense)
                {
                    case CreatureType.Digital: return 0.5f;
                    case CreatureType.Mecanico: return 0.5f;
                    case CreatureType.Entropia: return 0.5f;
                    case CreatureType.Organico: return 2f;
                    case CreatureType.Fosil: return 0.5f;
                    case CreatureType.Nexo: return 2f;
                    default: return 1f;
                }

            case CreatureType.Estrategia:
                switch (defense)
                {
                    case CreatureType.Digital: return 0.5f;
                    case CreatureType.Entropia: return 2f;
                    case CreatureType.Organico: return 2f;
                    case CreatureType.Estrategia: return 0.5f;
                    case CreatureType.Vector: return 0.5f;
                    case CreatureType.Mente: return 0.5f;
                    default: return 1f;
                }

            case CreatureType.Estructura:
                switch (defense)
                {
                    case CreatureType.Digital: return 0.5f;
                    case CreatureType.Entropia: return 0.5f;
                    case CreatureType.Fosil: return 2f;
                    case CreatureType.Vector: return 2f;
                    case CreatureType.Conducto: return 2f;
                    default: return 1f;
                }

            case CreatureType.Vector:
                switch (defense)
                {
                    case CreatureType.Armonia: return 2f;
                    case CreatureType.Estructura: return 0.5f;
                    case CreatureType.Mente: return 2f;
                    default: return 1f;
                }

            case CreatureType.Mente:
                switch (defense)
                {
                    case CreatureType.Armonia: return 0.5f;
                    case CreatureType.Organico: return 2f;
                    case CreatureType.Estrategia: return 2f;
                    case CreatureType.Vector: return 0.5f;
                    case CreatureType.Nexo: return 0.5f;
                    default: return 1f;
                }

            case CreatureType.Nexo:
                switch (defense)
                {
                    case CreatureType.Digital: return 0.5f;
                    case CreatureType.Armonia: return 0.5f;
                    case CreatureType.Entropia: return 0.5f;
                    case CreatureType.Fosil: return 0.5f;
                    case CreatureType.Mente: return 2f;
                    case CreatureType.Conducto: return 2f;
                    default: return 1f;
                }

            case CreatureType.Conducto:
                switch (defense)
                {
                    case CreatureType.Digital: return 2f;
                    case CreatureType.Mecanico: return 2f;
                    case CreatureType.Entropia: return 0.5f;
                    case CreatureType.Estructura: return 0.5f;
                    case CreatureType.Nexo: return 0.5f;
                    default: return 1f;
                }
        }

        return 1f;
    }
}