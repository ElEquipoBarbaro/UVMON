using UnityEngine;

/// <summary>Abstraccion de aleatoriedad (spec sec 33) para poder inyectar valores deterministas en pruebas.</summary>
public interface IRandomProvider
{
    float Range(float minInclusive, float maxInclusive);
}

/// <summary>Implementacion de produccion: usa UnityEngine.Random, sin semilla fija.</summary>
public class UnityRandomProvider : IRandomProvider
{
    public static readonly UnityRandomProvider Instance = new UnityRandomProvider();

    public float Range(float minInclusive, float maxInclusive)
    {
        return Random.Range(minInclusive, maxInclusive);
    }
}
