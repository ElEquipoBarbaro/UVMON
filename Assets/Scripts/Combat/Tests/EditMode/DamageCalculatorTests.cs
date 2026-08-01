using NUnit.Framework;
using UnityEngine;

/// <summary>Prompts 13 y 14 / spec sec 24-28, 35-36.</summary>
public class DamageCalculatorTests
{
    [Test]
    public void EjemploCompleto_100x06x08x105_Da50Punto4()
    {
        // valorImpacto=50 (<=80 => impacta), variacion=1.05 -> mismo ejemplo de la sec 27.
        FakeRandomProvider random = new FakeRandomProvider(50f, 1.05f);

        DamageCalculator.Result result = DamageCalculator.Calculate(
            qteExitoso: true,
            danoBase: 100,
            porcentajeAtaque: 60f,
            porcentajeAcertividad: 80f,
            vidaGlobalActualEnemigo: 999,
            random: random
        );

        Assert.IsTrue(result.ataqueImpacta);
        Assert.IsFalse(result.esCritico);
        Assert.AreEqual(50.4f, result.danoFinal, 0.001f);
        Assert.AreEqual(50, result.danoFinalEntero);
    }

    [Test]
    public void QteFallido_DanoCero_NoGeneraAcertividadNiVariacion()
    {
        FakeRandomProvider random = new FakeRandomProvider(0f, 1f);

        DamageCalculator.Result result = DamageCalculator.Calculate(
            qteExitoso: false,
            danoBase: 100,
            porcentajeAtaque: 60f,
            porcentajeAcertividad: 80f,
            vidaGlobalActualEnemigo: 999,
            random: random
        );

        Assert.AreEqual(0, result.danoFinalEntero);
        Assert.IsFalse(result.ataqueImpacta);
        Assert.AreEqual(0, random.CallCount, "QTE fallido no debe generar valorImpacto ni variacion.");
    }

    [Test]
    public void AcertividadFallida_DanoCero_NoGeneraVariacion()
    {
        // valorImpacto=90 > 80 => falla la acertividad.
        FakeRandomProvider random = new FakeRandomProvider(90f, 1f);

        DamageCalculator.Result result = DamageCalculator.Calculate(
            qteExitoso: true,
            danoBase: 100,
            porcentajeAtaque: 60f,
            porcentajeAcertividad: 80f,
            vidaGlobalActualEnemigo: 999,
            random: random
        );

        Assert.AreEqual(0, result.danoFinalEntero);
        Assert.IsFalse(result.ataqueImpacta);
        Assert.AreEqual(1, random.CallCount, "Solo debe consumirse valorImpacto, no variacion.");
    }

    [Test]
    public void DanoNuncaEsNegativo()
    {
        FakeRandomProvider random = new FakeRandomProvider(0f, 0.9f);

        DamageCalculator.Result result = DamageCalculator.Calculate(
            qteExitoso: true,
            danoBase: 0,
            porcentajeAtaque: 0f,
            porcentajeAcertividad: 100f,
            vidaGlobalActualEnemigo: 999,
            random: random
        );

        Assert.GreaterOrEqual(result.danoFinal, 0f);
        Assert.GreaterOrEqual(result.danoFinalEntero, 0);
    }

    [Test]
    public void Critico_QteEImpactoExitosos_UsaVidaGlobalActual()
    {
        // porcentajeAtaque >= 100 + QTE e impacto exitosos => danoFinal = vidaGlobalActualEnemigo.
        FakeRandomProvider random = new FakeRandomProvider(10f);

        DamageCalculator.Result result = DamageCalculator.Calculate(
            qteExitoso: true,
            danoBase: 100,
            porcentajeAtaque: 100f,
            porcentajeAcertividad: 35f,
            vidaGlobalActualEnemigo: 47,
            random: random
        );

        Assert.IsTrue(result.esCritico);
        Assert.AreEqual(47, result.danoFinalEntero);
        Assert.AreEqual(1, random.CallCount, "El critico no debe generar variacionAleatoria.");
    }

    [Test]
    public void Critico_ConQteFallido_NoDerrota()
    {
        FakeRandomProvider random = new FakeRandomProvider(10f);

        DamageCalculator.Result result = DamageCalculator.Calculate(
            qteExitoso: false,
            danoBase: 100,
            porcentajeAtaque: 100f,
            porcentajeAcertividad: 35f,
            vidaGlobalActualEnemigo: 47,
            random: random
        );

        Assert.IsFalse(result.esCritico);
        Assert.AreEqual(0, result.danoFinalEntero);
    }

    [Test]
    public void Critico_ConAcertividadFallida_NoDerrota()
    {
        // valorImpacto=90 > porcentajeAcertividad(35) => falla la acertividad, nunca llega al critico.
        FakeRandomProvider random = new FakeRandomProvider(90f);

        DamageCalculator.Result result = DamageCalculator.Calculate(
            qteExitoso: true,
            danoBase: 100,
            porcentajeAtaque: 100f,
            porcentajeAcertividad: 35f,
            vidaGlobalActualEnemigo: 47,
            random: random
        );

        Assert.IsFalse(result.esCritico);
        Assert.IsFalse(result.ataqueImpacta);
        Assert.AreEqual(0, result.danoFinalEntero);
    }

    [Test]
    public void PorcentajeAtaqueMenorA100_NuncaEsCritico()
    {
        FakeRandomProvider random = new FakeRandomProvider(10f, 1f);

        DamageCalculator.Result result = DamageCalculator.Calculate(
            qteExitoso: true,
            danoBase: 100,
            porcentajeAtaque: 99.9f,
            porcentajeAcertividad: 100f,
            vidaGlobalActualEnemigo: 47,
            random: random
        );

        Assert.IsFalse(result.esCritico);
    }
}
