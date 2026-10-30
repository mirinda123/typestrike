using System;
using System.Collections.Generic;

namespace TypeGameMod
{
    // No session state changes until the whole shared-charge packet validates.
    internal sealed class DuelPacket
    {
        internal bool Host, Ready, ShiftHeld;
        internal int Role, Winner, Speed;
        internal Guid Token, AcknowledgedToken;
        internal uint Sequence, Round, Scene, SpeedSequence, FireAck, SkillAck, Blocks, Hits;
        internal DuelPose Pose, OtherPose;
        internal List<DuelFire> Fires;
        internal List<DuelShot> Shots;
        internal RoleSkill[] Skills;
        internal WeaponAmmo[] Armories;
        internal DuelHealth Health;
        internal List<SkillAction> Actions;
    }
}
