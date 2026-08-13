using System;
using System.Numerics;
using FreeGhost.Core;
using NUnit.Framework;

namespace FreeGhost.Core.Tests;

[TestFixture]
public sealed class GhostPositionCodecTests
{
    [TestCase(10f, 2f, -4f)]
    [TestCase(-25f, 15f, 40f)]
    [TestCase(0.1f, 0.6f, -0.1f)]
    [TestCase(100f, -20f, 75f)]
    public void OrdinaryPositionsRoundTrip(float x, float y, float z)
    {
        Vector3 center = new(3f, 7f, -2f);
        Vector3 desired = center + new Vector3(x, y, z);

        AssertEncode(center, desired, null, out GhostEncoding encoding);
        Assert.That(Vector3.Distance(encoding.DecodedPosition, desired), Is.LessThan(0.2f));
        AssertFinite(encoding);
    }

    [TestCase(0f, 100f, 0f)]
    [TestCase(0f, -100f, 0f)]
    [TestCase(0.0001f, 50f, -0.0001f)]
    public void VerticalDirectionsRemainFinite(float x, float y, float z)
    {
        AssertEncode(Vector3.Zero, new Vector3(x, y, z), null, out GhostEncoding encoding);

        AssertFinite(encoding);
        Assert.That(encoding.Error, Is.LessThan(0.7f));
    }

    [Test]
    public void MovingTargetReencodesSameWorldPosition()
    {
        Vector3 desired = new(123f, 45f, -67f);
        AssertEncode(Vector3.Zero, desired, null, out GhostEncoding first);
        AssertEncode(new Vector3(20f, -3f, 11f), desired, first.LookValues, out GhostEncoding moved);

        Assert.That(Vector3.Distance(first.DecodedPosition, desired), Is.LessThan(0.25f));
        Assert.That(Vector3.Distance(moved.DecodedPosition, desired), Is.LessThan(0.25f));
    }

    [Test]
    public void SwitchingTargetsKeepsWorldPosition()
    {
        Vector3 desired = new(-33f, 12f, 91f);
        AssertEncode(new Vector3(4f, 1f, 2f), desired, null, out GhostEncoding first);
        AssertEncode(new Vector3(-200f, 30f, 18f), desired, first.LookValues, out GhostEncoding second);

        Assert.That(Vector3.Distance(first.DecodedPosition, second.DecodedPosition), Is.LessThan(0.5f));
    }

    [TestCase(0f, 0f, 0f)]
    [TestCase(0.1f, 0.1f, 0.1f)]
    [TestCase(0f, -0.49f, 0f)]
    public void VeryClosePositionsRemainRepresentable(float x, float y, float z)
    {
        AssertEncode(Vector3.Zero, new Vector3(x, y, z), null, out GhostEncoding encoding);

        AssertFinite(encoding);
        Assert.That(encoding.Error, Is.LessThan(0.01f));
    }

    [Test]
    public void OneKilometerPositionHasBoundedQuantizationError()
    {
        Vector3 desired = Vector3.Normalize(new Vector3(1f, 0.25f, -0.75f)) * 1000f;
        AssertEncode(Vector3.Zero, desired, null, out GhostEncoding encoding);

        AssertFinite(encoding);
        Assert.That(encoding.Error, Is.LessThan(2f));
    }

    [Test]
    public void DistanceBeyondHalfRangeFailsSafely()
    {
        Vector3 desired = new(100000f, 5000f, -25000f);
        Assert.That(GhostPositionCodec.TrySolveDirection(Vector3.Zero, desired, out Vector3 direction), Is.True);
        Vector2 look = GhostPositionCodec.DirectionToLook(direction);

        Assert.That(GhostPositionCodec.TryEncodeLook(Vector3.Zero, desired, look, null, out _), Is.False);
    }

    [Test]
    public void InvalidValuesAreRejected()
    {
        Assert.That(GhostPositionCodec.TrySolveDirection(new Vector3(float.NaN, 0f, 0f), Vector3.Zero, out _), Is.False);
        Assert.That(GhostPositionCodec.TrySolveDirection(Vector3.Zero, new Vector3(float.PositiveInfinity, 0f, 0f), out _), Is.False);
        Assert.That(GhostPositionCodec.TryEncodeLook(Vector3.Zero, Vector3.One, new Vector2(float.NaN, 0f), null, out _), Is.False);
    }

    [Test]
    public void YawIsUnwrappedNearPreviousValue()
    {
        Assert.That(GhostPositionCodec.UnwrapYaw(-179f, 179f), Is.EqualTo(181f));
        Assert.That(GhostPositionCodec.UnwrapYaw(179f, -179f), Is.EqualTo(-181f));
    }

    [Test]
    public void DecoderMatchesPeakTwoCardinalFormula()
    {
        Vector3 center = new(1f, 2f, 3f);
        Vector3 decoded = GhostPositionCodec.Decode(center, Vector2.Zero, 4f);

        Assert.That(decoded.X, Is.EqualTo(1f).Within(1e-6f));
        Assert.That(decoded.Y, Is.EqualTo(2.5f).Within(1e-6f));
        Assert.That(decoded.Z, Is.EqualTo(-1f).Within(1e-6f));
    }

    [Test]
    public void UpOffsetIsFixedInWorldSpace()
    {
        Vector3 center = new(1f, 2f, 3f);
        Vector3 decoded = GhostPositionCodec.Decode(center, new Vector2(0f, 45f), 4f);

        Assert.That(decoded.X, Is.EqualTo(1f).Within(1e-6f));
        Assert.That(decoded.Y, Is.EqualTo(2.5f - MathF.Sqrt(8f)).Within(1e-5f));
        Assert.That(decoded.Z, Is.EqualTo(3f - MathF.Sqrt(8f)).Within(1e-5f));
    }

    [Test]
    public void PositionAtWorldUpOffsetUsesZeroZoom()
    {
        Vector3 center = new(3f, 7f, -2f);
        Vector3 desired = center + Vector3.UnitY * GhostPositionCodec.GhostUpOffset;

        AssertEncode(center, desired, null, out GhostEncoding encoding);

        Assert.That(encoding.Zoom, Is.Zero);
        Assert.That(encoding.DecodedPosition, Is.EqualTo(desired));
    }

    [Test]
    public void EncodingUsesNearestHalfLookAndProjectedDistance()
    {
        Random random = new(0x46524748);
        for (int i = 0; i < 1000; i++)
        {
            Vector3 center = RandomVector(random, 100f);
            Vector3 desired = center + RandomVector(random, 1000f);
            Assert.That(GhostPositionCodec.TrySolveDirection(center, desired, out Vector3 direction), Is.True);
            Vector2 seed = GhostPositionCodec.DirectionToLook(direction);
            Assert.That(GhostPositionCodec.TryEncodeLook(center, desired, seed, null, out GhostEncoding encoded), Is.True);

            Vector2 expectedLook = new(HalfPrecision.Quantize(seed.X), HalfPrecision.Quantize(seed.Y));
            Vector3 quantizedDirection = GhostPositionCodec.DirectionFromLook(expectedLook);
            Vector3 travel = desired - center - Vector3.UnitY * GhostPositionCodec.GhostUpOffset;
            float expectedZoom = HalfPrecision.Quantize(MathF.Max(0f, -Vector3.Dot(travel, quantizedDirection)));

            Assert.That(encoded.LookValues, Is.EqualTo(expectedLook), $"look sample {i}");
            Assert.That(encoded.Zoom, Is.EqualTo(expectedZoom), $"zoom sample {i}");
            AssertFinite(encoded);
        }
    }

    private static Vector3 RandomVector(Random random, float extent)
    {
        float Next() => ((float)random.NextDouble() * 2f - 1f) * extent;
        return new Vector3(Next(), Next(), Next());
    }

    private static void AssertEncode(
        Vector3 center,
        Vector3 desired,
        Vector2? previousLook,
        out GhostEncoding encoding)
    {
        Assert.That(GhostPositionCodec.TrySolveDirection(center, desired, out Vector3 direction), Is.True);
        Vector2 gameLook = GhostPositionCodec.DirectionToLook(direction);
        Assert.That(
            GhostPositionCodec.TryEncodeLook(center, desired, gameLook, previousLook, out encoding),
            Is.True);
    }

    private static void AssertFinite(GhostEncoding encoding)
    {
        Assert.That(GhostPositionCodec.IsFinite(encoding.LookValues), Is.True);
        Assert.That(GhostPositionCodec.IsFinite(encoding.DecodedPosition), Is.True);
        Assert.That(GhostPositionCodec.IsFinite(encoding.Zoom), Is.True);
        Assert.That(GhostPositionCodec.IsFinite(encoding.Error), Is.True);
    }
}
