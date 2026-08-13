using System;
using System.Numerics;

namespace FreeGhost.Core;

public static class FreeFlightMath
{
    public static bool TryClampToRadius(
        Vector3 origin,
        Vector3 desiredPosition,
        float maxDistance,
        out Vector3 clampedPosition)
    {
        clampedPosition = origin;
        if (!GhostPositionCodec.IsFinite(origin) || !GhostPositionCodec.IsFinite(desiredPosition) ||
            !GhostPositionCodec.IsFinite(maxDistance) || maxDistance < 0f)
        {
            return false;
        }

        Vector3 offset = desiredPosition - origin;
        float distanceSquared = offset.LengthSquared();
        float maxDistanceSquared = maxDistance * maxDistance;
        if (!GhostPositionCodec.IsFinite(distanceSquared) || !GhostPositionCodec.IsFinite(maxDistanceSquared))
            return false;

        if (distanceSquared <= maxDistanceSquared)
        {
            clampedPosition = desiredPosition;
            return true;
        }

        float distance = MathF.Sqrt(distanceSquared);
        clampedPosition = origin + offset * (maxDistance / distance);
        return GhostPositionCodec.IsFinite(clampedPosition);
    }
}
