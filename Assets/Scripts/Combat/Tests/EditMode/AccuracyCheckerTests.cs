using NUnit.Framework;

/// <summary>Prompt 12 / spec sec 23, 35 (11-13).</summary>
public class AccuracyCheckerTests
{
    [Test]
    public void QteFallido_NoGeneraValorImpacto_YSiempreFalla()
    {
        FakeRandomProvider random = new FakeRandomProvider(999f);

        AccuracyChecker.Result result = AccuracyChecker.Evaluate(false, 100f, random);

        Assert.IsFalse(result.ataqueImpacta);
        Assert.AreEqual(0, random.CallCount, "No debe generarse valorImpacto si el QTE fallo.");
    }

    // porcentajeAcertividad, valorImpacto, resultado esperado
    [TestCase(0f, 0f, true, Description = "Acertividad 0: solo un valorImpacto exactamente 0 impacta.")]
    [TestCase(0f, 0.5f, false, Description = "Acertividad 0: cualquier valor > 0 falla.")]
    [TestCase(20f, 20f, true, Description = "Acertividad 20: el borde exacto impacta.")]
    [TestCase(20f, 20.1f, false, Description = "Acertividad 20: justo por encima del borde falla.")]
    [TestCase(80f, 80f, true, Description = "Acertividad 80: el borde exacto impacta.")]
    [TestCase(80f, 80.1f, false, Description = "Acertividad 80: justo por encima del borde falla.")]
    [TestCase(100f, 100f, true, Description = "Acertividad 100: incluso el maximo valorImpacto impacta.")]
    [TestCase(100f, 0f, true, Description = "Acertividad 100: cualquier valorImpacto impacta.")]
    public void Evaluate_ComparaValorImpactoContraAcertividad(float porcentajeAcertividad, float valorImpacto, bool esperado)
    {
        FakeRandomProvider random = new FakeRandomProvider(valorImpacto);

        AccuracyChecker.Result result = AccuracyChecker.Evaluate(true, porcentajeAcertividad, random);

        Assert.AreEqual(esperado, result.ataqueImpacta);
        Assert.AreEqual(valorImpacto, result.valorImpacto);
    }

    [Test]
    public void ValorImpacto_SeGeneraUnaSolaVez()
    {
        FakeRandomProvider random = new FakeRandomProvider(50f);

        AccuracyChecker.Evaluate(true, 80f, random);

        Assert.AreEqual(1, random.CallCount);
    }

    [TestCase(-10f, 0f)]
    [TestCase(150f, 100f)]
    public void PorcentajeAcertividad_SeLimitaA0_100(float entrada, float esperadoClamp)
    {
        FakeRandomProvider random = new FakeRandomProvider(esperadoClamp);

        AccuracyChecker.Result result = AccuracyChecker.Evaluate(true, entrada, random);

        Assert.AreEqual(esperadoClamp, result.porcentajeAcertividad);
    }
}
