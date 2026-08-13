using System.Numerics;
using FreeGhost.Core;
using NUnit.Framework;

namespace FreeGhost.Core.Tests;

[TestFixture]
public sealed class FreeFlightMathTests
{
    [Test]
    public void PositionInsideRadiusIsUnchanged()
    {
        Vector3 origin = new(10f, -2f, 5f);
        Vector3 desired = origin + new Vector3(100f, 200f, -300f);

        Assert.That(FreeFlightMath.TryClampToRadius(origin, desired, 1000f, out Vector3 clamped), Is.True);
        Assert.That(clamped, Is.EqualTo(desired));
    }

    [Test]
    public void PositionOutsideRadiusIsClampedToSphere()
    {
        Vector3 origin = new(10f, -2f, 5f);
        Vector3 desired = origin + new Vector3(3000f, 4000f, 0f);

        Assert.That(FreeFlightMath.TryClampToRadius(origin, desired, 1000f, out Vector3 clamped), Is.True);
        Assert.That(Vector3.Distance(origin, clamped), Is.EqualTo(1000f).Within(1e-3f));
        Assert.That(Vector3.Normalize(clamped - origin), Is.EqualTo(Vector3.Normalize(desired - origin)).Using(VectorComparer.Instance));
    }

    [Test]
    public void InvalidBoundaryInputIsRejected()
    {
        Assert.That(FreeFlightMath.TryClampToRadius(Vector3.Zero, Vector3.One, -1f, out _), Is.False);
        Assert.That(FreeFlightMath.TryClampToRadius(Vector3.Zero, new Vector3(float.NaN, 0f, 0f), 1000f, out _), Is.False);
    }

    private sealed class VectorComparer : System.Collections.Generic.IEqualityComparer<Vector3>
    {
        public static readonly VectorComparer Instance = new();

        public bool Equals(Vector3 x, Vector3 y) => Vector3.Distance(x, y) <= 1e-6f;
        public int GetHashCode(Vector3 obj) => obj.GetHashCode();
    }
}
