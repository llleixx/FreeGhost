using System;

namespace FreeGhost.Core;

public static class HalfPrecision
{
    public const float MaxFinite = 65504f;

    public static float Quantize(float value) => ToSingle(ToHalfBits(value));

    public static ushort ToHalfBits(float value)
    {
        uint bits = unchecked((uint)BitConverter.SingleToInt32Bits(value));
        uint sign = (bits >> 16) & 0x8000u;
        uint exponent = (bits >> 23) & 0xffu;
        uint mantissa = bits & 0x7fffffu;

        if (exponent == 0xffu)
        {
            if (mantissa == 0)
                return (ushort)(sign | 0x7c00u);

            uint payload = mantissa >> 13;
            return (ushort)(sign | 0x7c00u | payload | (payload == 0 ? 1u : 0u));
        }

        int halfExponent = (int)exponent - 127 + 15;
        if (halfExponent >= 31)
            return (ushort)(sign | 0x7c00u);

        if (halfExponent <= 0)
        {
            if (halfExponent < -10)
                return (ushort)sign;

            mantissa |= 0x800000u;
            int shift = 14 - halfExponent;
            uint result = mantissa >> shift;
            uint remainder = mantissa & ((1u << shift) - 1u);
            uint halfway = 1u << (shift - 1);
            if (remainder > halfway || (remainder == halfway && (result & 1u) != 0))
                result++;

            return (ushort)(sign | result);
        }

        uint roundedMantissa = mantissa >> 13;
        uint roundRemainder = mantissa & 0x1fffu;
        if (roundRemainder > 0x1000u || (roundRemainder == 0x1000u && (roundedMantissa & 1u) != 0))
        {
            roundedMantissa++;
            if (roundedMantissa == 0x400u)
            {
                roundedMantissa = 0;
                halfExponent++;
                if (halfExponent >= 31)
                    return (ushort)(sign | 0x7c00u);
            }
        }

        return (ushort)(sign | ((uint)halfExponent << 10) | roundedMantissa);
    }

    public static float ToSingle(ushort value)
    {
        uint sign = (uint)(value & 0x8000) << 16;
        uint exponent = (uint)(value >> 10) & 0x1fu;
        uint mantissa = (uint)value & 0x3ffu;
        uint bits;

        if (exponent == 0)
        {
            if (mantissa == 0)
            {
                bits = sign;
            }
            else
            {
                int normalizedExponent = -14;
                while ((mantissa & 0x400u) == 0)
                {
                    mantissa <<= 1;
                    normalizedExponent--;
                }

                mantissa &= 0x3ffu;
                bits = sign | (uint)(normalizedExponent + 127) << 23 | mantissa << 13;
            }
        }
        else if (exponent == 0x1fu)
        {
            bits = sign | 0x7f800000u | mantissa << 13;
        }
        else
        {
            bits = sign | (exponent + 112u) << 23 | mantissa << 13;
        }

        return BitConverter.Int32BitsToSingle(unchecked((int)bits));
    }
}
