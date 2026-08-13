using System;
using System.Numerics;

namespace FreeGhost.Core;

public static class GhostPositionCodec
{
    public const float GhostUpOffset = 0.5f;
    private const float Epsilon = 1e-8f;
    private const float RadToDeg = 180f / MathF.PI;
    private const float DegToRad = MathF.PI / 180f;

    public static bool TrySolveDirection(
        Vector3 targetCenter,
        Vector3 desiredPosition,
        out Vector3 direction)
    {
        direction = Vector3.Zero;
        if (!IsFinite(targetCenter) || !IsFinite(desiredPosition))
            return false;

        Vector3 travel = desiredPosition - targetCenter - Vector3.UnitY * GhostUpOffset;
        if (!TryNormalize(-travel, out direction))
        {
            direction = Vector3.UnitZ;
        }

        return IsFinite(direction);
    }

    public static bool TryEncodeAroundLook(
        Vector3 targetCenter,
        Vector3 desiredPosition,
        Vector2 seedLook,
        Vector2? previousLook,
        out GhostEncoding encoding)
    {
        encoding = default;
        if (!IsFinite(targetCenter) || !IsFinite(desiredPosition) || !IsFinite(seedLook))
            return false;

        if (previousLook.HasValue)
            seedLook.X = UnwrapYaw(seedLook.X, previousLook.Value.X);

        Vector2 center = new(HalfPrecision.Quantize(seedLook.X), HalfPrecision.Quantize(seedLook.Y));
        if (!IsFinite(center))
            return false;

        float[] yaws = { center.X, HalfPrecision.PreviousFinite(center.X), HalfPrecision.NextFinite(center.X) };
        float[] pitches = { center.Y, HalfPrecision.PreviousFinite(center.Y), HalfPrecision.NextFinite(center.Y) };
        bool found = false;
        GhostEncoding best = default;

        foreach (float yaw in yaws)
        {
            foreach (float rawPitch in pitches)
            {
                float pitch = MathF.Max(-90f, MathF.Min(90f, rawPitch));
                if (!TryEvaluateQuantizedLook(
                        targetCenter,
                        desiredPosition,
                        new Vector2(yaw, pitch),
                        out GhostEncoding candidate))
                {
                    continue;
                }

                if (!found || candidate.Error < best.Error)
                {
                    found = true;
                    best = candidate;
                }
            }
        }

        encoding = best;
        return found;
    }

    public static Vector3 Decode(Vector3 targetCenter, Vector2 lookValues, float zoom)
    {
        Vector3 direction = DirectionFromLook(lookValues);
        return targetCenter - direction * zoom + Vector3.UnitY * GhostUpOffset;
    }

    public static Vector3 DirectionFromLook(Vector2 lookValues)
    {
        float yaw = lookValues.X * DegToRad;
        float pitch = lookValues.Y * DegToRad;
        float cosPitch = MathF.Cos(pitch);
        return Vector3.Normalize(new Vector3(
            MathF.Sin(yaw) * cosPitch,
            MathF.Sin(pitch),
            MathF.Cos(yaw) * cosPitch));
    }

    public static Vector2 DirectionToLook(Vector3 direction)
    {
        if (!TryNormalize(direction, out Vector3 normalized))
            return Vector2.Zero;

        float yaw = MathF.Atan2(normalized.X, normalized.Z) * RadToDeg;
        float pitch = MathF.Asin(MathF.Max(-1f, MathF.Min(1f, normalized.Y))) * RadToDeg;
        return new Vector2(yaw, pitch);
    }

    public static float UnwrapYaw(float yaw, float reference)
    {
        if (!IsFinite(yaw) || !IsFinite(reference))
            return yaw;

        return yaw + 360f * MathF.Round((reference - yaw) / 360f);
    }

    public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    public static bool IsFinite(Vector2 value) => IsFinite(value.X) && IsFinite(value.Y);
    public static bool IsFinite(Vector3 value) => IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);

    private static bool TryEvaluateQuantizedLook(
        Vector3 targetCenter,
        Vector3 desiredPosition,
        Vector2 lookValues,
        out GhostEncoding encoding)
    {
        encoding = default;
        Vector2 quantizedLook = new(HalfPrecision.Quantize(lookValues.X), HalfPrecision.Quantize(lookValues.Y));
        if (!IsFinite(quantizedLook))
            return false;

        Vector3 direction = DirectionFromLook(quantizedLook);
        Vector3 travel = desiredPosition - targetCenter - Vector3.UnitY * GhostUpOffset;
        float zoom = HalfPrecision.Quantize(MathF.Max(0f, -Vector3.Dot(travel, direction)));
        if (!IsFinite(direction) || !IsFinite(zoom))
            return false;

        Vector3 decoded = targetCenter - direction * zoom + Vector3.UnitY * GhostUpOffset;
        float error = Vector3.Distance(decoded, desiredPosition);
        if (!IsFinite(decoded) || !IsFinite(error))
            return false;

        encoding = new GhostEncoding(quantizedLook, zoom, decoded, error);
        return true;
    }

    private static bool TryNormalize(Vector3 value, out Vector3 normalized)
    {
        float lengthSquared = value.LengthSquared();
        if (!IsFinite(lengthSquared) || lengthSquared <= Epsilon * Epsilon)
        {
            normalized = Vector3.Zero;
            return false;
        }

        normalized = value / MathF.Sqrt(lengthSquared);
        return IsFinite(normalized);
    }
}
