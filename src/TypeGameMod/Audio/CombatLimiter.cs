using System;

namespace TypeGameMod
{
    internal static class CombatLimiter
    {
        // Smooth overload above -4.3dB; even simultaneous shots stay below full scale.
        internal static short Apply(int value)
        {
            long absolute = Math.Abs((long)value);
            if (absolute <= 20000)
                return (short)value;
            int limited = 20000 + (int)(11128 * (1 - Math.Exp(-(absolute - 20000) / 11128.0)));
            return (short)(value < 0 ? -limited : limited);
        }
    }
}
