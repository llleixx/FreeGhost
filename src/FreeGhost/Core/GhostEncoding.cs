using System.Numerics;

namespace FreeGhost.Core;

public readonly struct GhostEncoding
{
    public GhostEncoding(Vector2 lookValues, float zoom, Vector3 decodedPosition, float error)
    {
        LookValues = lookValues;
        Zoom = zoom;
        DecodedPosition = decodedPosition;
        Error = error;
    }

    public Vector2 LookValues { get; }
    public float Zoom { get; }
    public Vector3 DecodedPosition { get; }
    public float Error { get; }
}
