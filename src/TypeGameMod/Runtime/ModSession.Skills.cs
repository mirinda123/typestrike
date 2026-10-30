using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        RoleSkill offlineSkill = new RoleSkill(0);
        readonly TypingController typing = new TypingController();
        NativeTypingBridge nativeTyping;
        NativeSkillBridge nativeSkills;
        uint skillRound;
        int skillRole = -1;

        void InitializeNativeBridges()
        {
            nativeTyping = new NativeTypingBridge(Read, Write, ControlBase, caveBase);
            nativeSkills = new NativeSkillBridge(Read, Write, ControlBase, assetBase, log);
        }

        void ResetOfflineSkill(uint role)
        {
            offlineSkill = new RoleSkill((int)role);
            typing.Reset();
            nativeSkills.ResetBlinkTracking();
            skillRound = 0;
            skillRole = (int)role;
            nativeTyping.SetTyping(false);
        }

        RoleSkill Skill(uint role)
        {
            return networkMatch == null ? offlineSkill : networkMatch.Skills[role];
        }

        bool DebugChargeSkill(bool active, uint role, InputFrame input, double now)
        {
            if (!active || !input.Focused || !input.DebugCharge)
                return false;
            RoleSkill skill = Skill(role);
            skill.Tick(now);
            if (skill.Shield)
                return false;
            // Debug uses the same reliable word confirmations as ordinary charging.
            for (int word = 0; word < 6 && !skill.Full; word++)
            {
                if (!ConfirmSkillWord(role, now))
                    return false;
            }
            typing.ReturnToMovement();
            input.ShiftPressed = false;
            return true;
        }

        bool ConfirmSkillWord(uint role, double now)
        {
            RoleSkill skill = Skill(role);
            return networkMatch == null ? skill.Match(skill.Word, now)
                                        : networkMatch.ConfirmSkillWord(skill.Word, now);
        }

        void ReadSkillCharacters(bool active, uint gameObject, uint role, InputFrame input, double now)
        {
            if (!active)
                typing.LeaveRound();
            RoleSkill skill = Skill(role);
            skill.Tick(now);
            bool held = active && input.Focused && input.ShiftHeld && typing.ControlsAllowed;
            if (networkMatch == null)
                skill.Hold(held, now);
            else
                networkMatch.UpdateShield(held, now);
            if (active && typing.Article == null)
                typing.SetArticle(nativeTyping.ReadArticle(gameObject, role, skill.Cycle));
            nativeTyping.ReadKeys(delegate(int key, int position) {
                if (!active || !input.Focused || !typing.HandleKey(key, position, skill.Shield, now))
                    return;
                ObserveArticleWords(role, position, now);
            });
            if (DebugChargeSkill(active, role, input, now))
                log("Debug shared skill charge ready role=" + role + " cycle=" + skill.Cycle);
            if (!typing.ControlsAllowed)
                input.ShiftPressed = false;
            nativeTyping.SetTyping(typing.Enabled);
            if (!typing.ControlsAllowed)
                Write(ControlBase + Control.VelocityX, new byte[16]);
        }

        void ObserveArticleWords(uint role, int position, double now)
        {
            RoleSkill skill = Skill(role);
            int words = typing.ObserveWords(position, skill);
            for (int i = 0; i < words && skill.CanCharge; i++)
            {
                if (ConfirmSkillWord(role, now))
                    log("Native article word matched role=" + role + " cycle=" + skill.Cycle +
                        " energy=" + skill.Energy.ToString("F1") + "/6 cursor=" + position);
            }
        }

        void OfflineSkillInput(uint gameObject, uint role, ref double x, ref double y, uint heading,
                               InputFrame input, double now)
        {
            RoleSkill skill = offlineSkill;
            skill.Tick(now);
            if (role == 1 && input.Focused && input.ShiftPressed && typing.ControlsAllowed)
            {
                var origin = new DuelPose { X = (float)x, Y = (float)y, Heading = (byte)heading,
                                            Weapon = (byte)equippedWeapon };
                DuelPose target;
                if (skill.Use(origin, now, MovementMap.City, out target))
                {
                    x = target.X;
                    y = target.Y;
                    log("Thief blink: destination=" + x.ToString("F1") + "," + y.ToString("F1") +
                        " energy=" + skill.Energy.ToString("F1"));
                }
            }
            skill.Hold(input.Focused && input.ShiftHeld && typing.ControlsAllowed, now);
            if (role == 1)
                nativeSkills.QueueBlink(gameObject, role, skill, now);
        }

        uint VisualWeapon(uint role)
        {
            return Skill(role).Shield ? 3u : (uint)equippedWeapon;
        }

        void SkillSnapshot(byte[] snapshot, uint role, bool active, double x, double y, double now)
        {
            RoleSkill own = Skill(role);
            own.Tick(now);
            RoleSkill police = networkMatch == null ? (role == 0 ? own : null) : networkMatch.Skills[0];
            nativeSkills.SetPoliceShield(active && police != null && police.Shield);
            nativeSkills.SetLocalShield(active && own.Shield);
            if (!active)
                typing.Feedback.Clear();
            var view =
                new SkillView { Own = own,
                                Thief =
                                    networkMatch == null ? (role == 1 ? own : null) : networkMatch.Skills[1],
                                Ammo = networkMatch == null ? offlineMatch.Ammo : networkMatch.Armories[role],
                                Weapon = equippedWeapon,
                                Active = active,
                                Typing = typing.Enabled,
                                Article = typing.Article,
                                Feedback = typing.Feedback,
                                PlayerX = x,
                                PlayerY = y };
            SkillHud.Write(snapshot, view, now);
        }
    }
}
