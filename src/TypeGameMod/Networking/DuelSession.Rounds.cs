using System;

namespace TypeGameMod
{
    internal sealed partial class DuelSession
    {
        void ResetRound(uint round)
        {
            Round = round;
            Winner = -1;
            RequestedScene = -1;
            Shots.Clear();
            pendingFireActions.Clear();
            fireSequence = localFireAck = remoteFireAck = 0;
            ShotsFired = PelletsFired = Hits = Blocks = 0;
            Health.Reset();
            Armories[0].Reset();
            Armories[1].Reset();
            reloadPressed = false;
            skillSequence = skillAck = 0;
            pendingSkillActions.Clear();
            Skills[0].Reset();
            Skills[1].Reset();
            supplyPressed = false;
            shiftHeld = shiftPressed = shieldRequested = false;
            localReady = remoteReady = false;
            lastFire = -100;
            Poses[0] = DuelPose.Spawn(0);
            Poses[1] = DuelPose.Spawn(1);
            previousPoses[0] = Poses[0];
            previousPoses[1] = Poses[1];
            inLobby = false;
            abortRequested = false;
            everPlaying = false;
        }

        void Lobby()
        {
            Armories[0].Reset();
            Armories[1].Reset();
            reloadPressed = false;
            inLobby = true;
            Winner = -1;
            Health.Reset();
            Hits = 0;
            Shots.Clear();
            pendingFireActions.Clear();
            pendingSkillActions.Clear();
            Skills[0].Reset();
            Skills[1].Reset();
            supplyPressed = false;
            localReady = remoteReady = false;
        }

        internal void Ready()
        {
            if (Winner >= 0)
                localReady = true;
        }
    }
}
