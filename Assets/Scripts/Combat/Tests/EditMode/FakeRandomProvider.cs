using System.Collections.Generic;

/// <summary>Proveedor de aleatoriedad controlable para pruebas (spec sec 33). Devuelve
/// valores predeterminados en el orden en que se piden y cuenta cuantas veces se llamo,
/// para poder comprobar que valorImpacto/variacionAleatoria se generan una unica vez.</summary>
public class FakeRandomProvider : IRandomProvider
{
    private readonly Queue<float> queuedValues;

    public int CallCount { get; private set; }

    public FakeRandomProvider(params float[] values)
    {
        queuedValues = new Queue<float>(values);
    }

    public float Range(float minInclusive, float maxInclusive)
    {
        CallCount++;
        return queuedValues.Count > 0 ? queuedValues.Dequeue() : minInclusive;
    }
}
