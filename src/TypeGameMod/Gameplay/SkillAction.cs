using System;

namespace TypeGameMod
{
    internal sealed class SkillAction
    {
        internal const byte RoleWord = 0, RoleUse = 1, SupplyUse = 2, ShieldRaise = 3, ShieldLower = 4;
        internal uint Id, Round, Cycle;
        internal byte Kind;
        internal string Word = "";
        internal DuelPose Origin;
    }
}
