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

}
