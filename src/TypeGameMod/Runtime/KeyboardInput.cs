using System;

namespace TypeGameMod
{
    internal sealed class InputFrame
    {
        internal bool Focused, Fire, ConfirmResult, Reload, Supply, ShiftHeld, ShiftPressed, Slower, Faster;
        internal bool DebugCharge;
        internal bool GameplayKeysHeld;
        internal int X, Y, Selection;
        internal uint Heading;
    }
    internal sealed class KeyboardInput
    {
        readonly Func<int, bool> down;
        bool previousAk, previousShotgun, previousSlower, previousFaster, previousReload, previousShift;
        bool previousSupply, previousDebugCharge, fireReady, resultReady;
        uint heading;
        internal KeyboardInput(Func<int, bool> isDown)
        {
            down = isDown;
        }

        internal InputFrame Read(bool focused, bool controls, bool resultScreen = false,
                                 TypingController typing = null)
        {
            bool ak = down(0x31), shotgun = down(0x32), slower = down(0xdb), faster = down(0xdd);
            bool reload = down(0x52), supply = down(0x45), shiftDown = down(0x10);
            bool shift = focused && shiftDown;
            bool enter = down(0x0d);
            bool w = down(0x57), a = down(0x41), s = down(0x53), d = down(0x44), tab = down(0x09);
            bool gameplayKeysHeld =
                ak || shotgun || reload || supply || shiftDown || enter || w || a || s || d || tab;
            if (typing != null)
            {
                typing.UpdateReleaseGate(gameplayKeysHeld);
                controls = controls && typing.ControlsAllowed;
            }
            bool debugCharge = down(0x11) && down(0x12) && down(0x78); // Ctrl + Alt + F9
            // A held typing/restart key must be released before it becomes a trigger.
            if (!focused || !controls)
                fireReady = false;
            else if (!enter)
                fireReady = true;
            // Result confirmation starts with a release on the result screen.
            // Consume it once; Windows character repeats never confirm a rematch.
            if (!focused || !resultScreen)
                resultReady = false;
            else if (!enter)
                resultReady = true;
            bool confirmResult = resultReady && enter;
            if (confirmResult)
                resultReady = false;
            var input = new InputFrame { Focused = focused,
                                         GameplayKeysHeld = gameplayKeysHeld,
                                         DebugCharge = focused && debugCharge && !previousDebugCharge,
                                         ConfirmResult = confirmResult,
                                         Reload = controls && reload && !previousReload,
                                         Supply = controls && supply && !previousSupply,
                                         ShiftHeld = shift,
                                         ShiftPressed = shift && !previousShift,
                                         Slower = slower && !previousSlower,
                                         Faster = faster && !previousFaster,
                                         Selection = controls && shotgun && !previousShotgun ? 2
                                                     : controls && ak && !previousAk         ? 1
                                                                                             : 0,
                                         X = controls ? (d ? 1 : 0) - (a ? 1 : 0) : 0,
                                         Y = controls ? (s ? 1 : 0) - (w ? 1 : 0) : 0,
                                         Fire = focused && controls && fireReady && enter };
            if (input.X == 0 && input.Y != 0)
                heading = (uint)(input.Y < 0 ? 0 : 4);
            if (input.X > 0)
                heading = (uint)(input.Y < 0 ? 1 : input.Y > 0 ? 3 : 2);
            if (input.X < 0)
                heading = (uint)(input.Y < 0 ? 7 : input.Y > 0 ? 5 : 6);
            input.Heading = heading;
            previousAk = ak;
            previousShotgun = shotgun;
            previousSlower = slower;
            previousFaster = faster;
            previousReload = reload;
            previousSupply = supply;
            previousShift = shift;
            // Observe the chord even outside play, so holding it across a focus or
            // scene change cannot silently charge the next round.
            previousDebugCharge = debugCharge;
            return input;
        }
    }
}
