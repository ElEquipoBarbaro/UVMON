using NUnit.Framework;

/// <summary>Prompts 15-17 / spec sec 29-30, 35 (17), 36 (25-27).</summary>
public class BodyPartTests
{
    private static BodyPart MakePart(int vidaMaxima = 50)
    {
        BodyPartDefinition definition = new BodyPartDefinition
        {
            idParte = "body",
            nombreParte = "Cuerpo",
            vidaMaxima = vidaMaxima,
            porcentajeAtaque = 60f,
            porcentajeAcertividad = 80f
        };

        return new BodyPart(definition);
    }

    [Test]
    public void VidaActual_NoBajaDeCero()
    {
        BodyPart part = MakePart(30);

        part.ApplyDamage(999);

        Assert.AreEqual(0, part.VidaActual);
    }

    [Test]
    public void EstadoDanado_SeMarcaUnaSolaVez_AlCruzarAcero()
    {
        BodyPart part = MakePart(30);

        bool primeraVez = part.ApplyDamage(30);
        bool segundaVez = part.ApplyDamage(10);

        Assert.IsTrue(primeraVez, "Debe reportar el cruce la primera vez que la vida llega a 0.");
        Assert.IsFalse(segundaVez, "Ataques posteriores no deben repetir el cambio de estado.");
        Assert.IsTrue(part.EstadoDanado);
    }

    [Test]
    public void DanoParcial_NoMarcaEstadoDanado()
    {
        BodyPart part = MakePart(30);

        bool justCrossed = part.ApplyDamage(10);

        Assert.IsFalse(justCrossed);
        Assert.IsFalse(part.EstadoDanado);
        Assert.AreEqual(20, part.VidaActual);
    }

    [Test]
    public void EsParteCritica_SoloConPorcentajeAtaque100OMas()
    {
        BodyPartDefinition critico = new BodyPartDefinition { porcentajeAtaque = 100f, vidaMaxima = 10 };
        BodyPartDefinition normal = new BodyPartDefinition { porcentajeAtaque = 99f, vidaMaxima = 10 };

        Assert.IsTrue(new BodyPart(critico).EsParteCritica);
        Assert.IsFalse(new BodyPart(normal).EsParteCritica);
    }
}
