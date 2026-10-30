using System;

namespace TypeGameMod
{
    internal sealed class MusicEnvelope
    {
        internal const float Base = .56f, Low = .20f;
        volatile int triggered;
        volatile bool used;
        internal void Trigger(int now)
        {
            triggered = now;
            used = true;
        }

        internal float Level(int now)
        {
            if (!used)
                return Base;
            uint elapsed = unchecked((uint)(now - triggered));
            if (elapsed < 110)
                return Low;
            if (elapsed >= 290)
                return Base;
            return Low + (Base - Low) * (elapsed - 110) / 180f;
        }
    }
}
