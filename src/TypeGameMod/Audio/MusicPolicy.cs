using System;

namespace TypeGameMod
{
    internal sealed class MusicPolicy
    {
        internal bool Triggered;
        internal bool ReplacesNative { get; private set; }
        internal bool Observe(bool armed, bool enabled, bool ready, bool inRound)
        {
            if (armed && enabled)
                Triggered = true;
            // Keep the selected track and native mute across results/menus, but
            // advance the music only while a round is actually being played.
            ReplacesNative = Triggered && enabled && ready;
            return ReplacesNative && inRound;
        }
    }
}
