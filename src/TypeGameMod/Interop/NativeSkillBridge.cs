using System;

namespace TypeGameMod
{
    // Owns the native blink mailbox and shield art pointers. Gameplay energy and
    // input modes stay outside this process-memory boundary.
    internal sealed class NativeSkillBridge
    {
        readonly Func<uint, int, byte[]> read;
        readonly Action<uint, byte[]> write;
        readonly Action<string> log;
        readonly uint control, assets;
        bool shieldVisual;
        uint blinkApplied, blinkRequest;
        double blinkQueuedAt;
        float queuedBlinkX, queuedBlinkY;

        internal NativeSkillBridge(Func<uint, int, byte[]> read, Action<uint, byte[]> write, uint control,
                                   uint assets, Action<string> log)
        {
            this.read = read;
            this.write = write;
            this.control = control;
            this.assets = assets;
            this.log = log;
        }

        internal void ResetBlinkTracking()
        {
            blinkApplied = 0;
        }

        internal void SetLocalShield(bool active)
        {
            write(control + Control.SkillActive, BitConverter.GetBytes(active ? 1u : 0u));
        }

        internal void QueueBlink(uint gameObject, uint role, RoleSkill skill, double now)
        {
            if (role != 1 || skill.BlinkSerial == blinkApplied)
                return;
            uint request = BitConverter.ToUInt32(read(control + Control.BlinkSequence, 4), 0),
                 ack = BitConverter.ToUInt32(read(control + Control.BlinkAcknowledged, 4), 0);
            if (request != ack)
                return;
            byte[] b = new byte[16];
            BinaryFields.Write(b, 0, skill.BlinkX);
            BinaryFields.Write(b, 4, skill.BlinkY);
            BinaryFields.Write(b, 8, gameObject);
            BinaryFields.Write(b, 12, role);
            write(control + Control.BlinkX, b);
            blinkRequest = request + 1;
            write(control + Control.BlinkSequence, BitConverter.GetBytes(blinkRequest));
            blinkApplied = skill.BlinkSerial;
            blinkQueuedAt = now;
            queuedBlinkX = skill.BlinkX;
            queuedBlinkY = skill.BlinkY;
            log("Blink queued on game UI thread serial=" + blinkApplied);
        }

        internal void ApplyPendingPose(ref DuelPose own, double now)
        {
            if (own.Heading > 7 || blinkRequest == 0 || now - blinkQueuedAt > .15)
                return;
            if (BitConverter.ToUInt32(read(control + Control.BlinkAcknowledged, 4), 0) != blinkRequest)
            {
                own.X = queuedBlinkX;
                own.Y = queuedBlinkY;
            }
        }

        internal void SetPoliceShield(bool policeShield)
        {
            if (policeShield == shieldVisual)
                return;
            shieldVisual = policeShield;
            write(control + Control.PoliceAkRun0,
                  BitConverter.GetBytes(
                      assets + (policeShield ? AssetLayout.PoliceShield0 : AssetLayout.PoliceArmed0)));
            write(control + Control.PoliceAkRun1,
                  BitConverter.GetBytes(
                      assets + (policeShield ? AssetLayout.PoliceShield1 : AssetLayout.PoliceArmed1)));
            write(control + Control.PoliceAkIdle,
                  BitConverter.GetBytes(assets + (policeShield ? AssetLayout.PoliceShieldIdleTable
                                                               : AssetLayout.PoliceArmedIdleTable)));
        }
    }
}
