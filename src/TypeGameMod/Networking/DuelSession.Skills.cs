namespace TypeGameMod
{
    internal sealed partial class DuelSession
    {
        internal void SkillKeys(bool held, bool pressed)
        {
            shiftHeld = held;
            shiftPressed = pressed;
        }

        internal bool ConfirmSkillWord(string word, double now)
        {
            if (!Playing || (!Host && pendingSkillActions.Count >= DuelProtocol.MaximumSkillActions))
                return false;
            var action =
                new SkillAction { Id = skillSequence + 1,      Round = Round, Cycle = Skills[Role].Cycle,
                                  Kind = SkillAction.RoleWord, Word = word,   Origin = Poses[Role] };
            if (!Skills[Role].Match(word, now))
                return false;
            skillSequence++;
            if (!Host)
                pendingSkillActions.Add(action);
            return true;
        }

        bool ApplySkillAction(SkillAction action, int role, double now)
        {
            if (action.Kind == SkillAction.ShieldRaise || action.Kind == SkillAction.ShieldLower)
            {
                if (role != 0)
                    return false;
                Skills[role].Hold(action.Kind == SkillAction.ShieldRaise, now);
                return true;
            }
            if (action.Cycle != Skills[role].Cycle)
                return false;
            if (action.Kind == SkillAction.SupplyUse)
                return action.Origin.Weapon == Poses[role].Weapon &&
                       Skills[role].Resupply(Armories[role], action.Origin.Weapon, now);
            if (action.Kind == SkillAction.RoleWord)
                return Skills[role].Match(action.Word, now);
            DuelPose target;
            if (!Skills[role].Use(action.Origin, now, movement, out target))
                return false;
            if (role == 1)
            {
                Poses[role] = target;
                previousPoses[role] = target;
            }
            return true;
        }

        internal void SupplyKey(bool pressed)
        {
            supplyPressed = pressed;
        }
        // Shield edges share the reliable action queue with words, so release ->
        // typing -> raise order survives batching, lost packets and retransmission.
        internal void UpdateShield(bool held, double now)
        {
            if (Role != 0)
                return;
            if (!Playing)
            {
                Skills[Role].Hold(false, now);
                shieldRequested = false;
                return;
            }
            held = held && !pendingSkillActions.Exists(
                               delegate(SkillAction action) { return action.Kind == SkillAction.SupplyUse; });
            if (held != shieldRequested)
            {
                if (!Host && pendingSkillActions.Count >= DuelProtocol.MaximumSkillActions)
                    return;
                var action =
                    new SkillAction { Id = ++skillSequence, Round = Round, Cycle = Skills[Role].Cycle,
                                      Kind = held ? SkillAction.ShieldRaise : SkillAction.ShieldLower,
                                      Origin = Poses[Role] };
                if (!Host)
                    pendingSkillActions.Add(action);
                shieldRequested = held;
            }
            Skills[Role].Hold(held, now);
        }
    }
}
