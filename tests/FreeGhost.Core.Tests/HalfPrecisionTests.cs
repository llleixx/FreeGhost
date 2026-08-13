using System;
using FreeGhost.Core;
using NUnit.Framework;

namespace FreeGhost.Core.Tests;

[TestFixture]
public sealed class HalfPrecisionTests
{
    [TestCase(0f)]
    [TestCase(-0f)]
    [TestCase(1f)]
    [TestCase(-2f)]
    [TestCase(0.333251953125f)]
    [TestCase(1000f)]
    [TestCase(65504f)]
    public void QuantizeMatchesSystemHalf(float value)
    {
        float expected = (float)(Half)value;
        Assert.That(HalfPrecision.Quantize(value), Is.EqualTo(expected));
    }

    [Test]
    public void OverflowBecomesInfinity()
    {
        Assert.That(HalfPrecision.Quantize(70000f), Is.EqualTo(float.PositiveInfinity));
        Assert.That(HalfPrecision.Quantize(-70000f), Is.EqualTo(float.NegativeInfinity));
    }

    [Test]
    public void NaNRemainsNaN()
    {
        Assert.That(float.IsNaN(HalfPrecision.Quantize(float.NaN)), Is.True);
    }

    [TestCase(1f, 0.99951171875f, 1.0009765625f)]
    [TestCase(-1f, -1.0009765625f, -0.99951171875f)]
    [TestCase(0f, -0.000000059604645f, 0.000000059604645f)]
    public void AdjacentFiniteValuesAreOrdered(float value, float expectedPrevious, float expectedNext)
    {
        Assert.That(HalfPrecision.PreviousFinite(value), Is.EqualTo(expectedPrevious));
        Assert.That(HalfPrecision.NextFinite(value), Is.EqualTo(expectedNext));
    }

    [Test]
    public void AdjacentFiniteValuesStayAtFiniteLimits()
    {
        Assert.That(HalfPrecision.PreviousFinite(-HalfPrecision.MaxFinite), Is.EqualTo(-HalfPrecision.MaxFinite));
        Assert.That(HalfPrecision.NextFinite(HalfPrecision.MaxFinite), Is.EqualTo(HalfPrecision.MaxFinite));
    }

    [Test]
    public void AdjacentFiniteValuesRejectNonFiniteInput()
    {
        Assert.That(float.IsNaN(HalfPrecision.PreviousFinite(float.PositiveInfinity)), Is.True);
        Assert.That(float.IsNaN(HalfPrecision.NextFinite(float.NaN)), Is.True);
    }
}
