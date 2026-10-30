using System;

namespace TypeGameMod
{
    internal sealed partial class ModSession
    {
        int thiefAkIdleFrame;
        int policeIdleFrame;
        int policeAkRunFrame;
        int policeShotgunRunFrame;
        int thiefAkRunFrame;
        int thiefShotgunIdleFrame;
        int thiefShotgunRunFrame;
        readonly PoliceIdleAnimation policeIdleAnimation = new PoliceIdleAnimation();
        readonly RunAnimation policeRunAnimation = new RunAnimation();
        readonly RunAnimation policeAkRunAnimation = new RunAnimation();
        readonly RunAnimation policeShotgunRunAnimation = new RunAnimation();
        readonly RunAnimation policeShieldRunAnimation = new RunAnimation();
        readonly ThiefIdleAnimation thiefIdleAnimation = new ThiefIdleAnimation();
        readonly RunAnimation thiefRunAnimation = new RunAnimation();
        readonly ThiefAkAnimation thiefAkAnimation = new ThiefAkAnimation();
        readonly RunAnimation thiefAkRunAnimation = new RunAnimation();
        readonly ThiefShotgunAnimation thiefShotgunIdleAnimation = new ThiefShotgunAnimation();
        readonly RunAnimation thiefShotgunRunAnimation = new RunAnimation();
        readonly TypingPromptAnimation typingPromptAnimation = new TypingPromptAnimation();
        CharacterVisualState ReadCharacterVisuals(int role, GameFrame frame, InputFrame input, bool active)
        {
            if (frame.HasNetworkPlayers && networkMatch != null)
            {
                DuelPose pose = networkMatch.Poses[role];
                return new CharacterVisualState { Active = active,
                                                  Moving = pose.Moving != 0,
                                                  Weapon = pose.Weapon,
                                                  Shield = role == 0 && networkMatch.Skills[0].Shield,
                                                  AkShots = networkMatch.Armories[role].Fired[GameRules.Ak47],
                                                  ShotgunShots =
                                                      networkMatch.Armories[role].Fired[GameRules.Shotgun] };
            }
            bool localRole = frame.Role == role;
            return new CharacterVisualState {
                Active = active && localRole,
                Moving = input.X != 0 || input.Y != 0,
                Weapon = equippedWeapon,
                Shield = role == 0 && offlineSkill.Shield,
                AkShots = localRole ? offlineMatch.Ammo.Fired[GameRules.Ak47] : 0,
                ShotgunShots = localRole ? offlineMatch.Ammo.Fired[GameRules.Shotgun] : 0
            };
        }

        void UpdateAnimations(GameFrame frame, InputFrame input, byte[] snapshot, bool inRound, double dt)
        {
            bool active = inRound && input.Focused;
            CharacterVisualState police = ReadCharacterVisuals(0, frame, input, active);
            CharacterVisualState thief = ReadCharacterVisuals(1, frame, input, active);
            bool typing = inRound && BitConverter.ToUInt32(snapshot, SnapshotLayout.Typing) == 1;
            int typingFrame = typingPromptAnimation.Update(typing, input.Focused ? dt : 0);
            BinaryFields.Write(snapshot, SnapshotLayout.TypingArtFrame, (uint)typingFrame);
            UpdatePoliceAnimation(police, dt);
            UpdateThiefAnimation(thief, dt);
            var effects = new FireEffectBuffer(snapshot);
            ThiefWeaponEffects(frame, effects, thief.Ak47, thief.Shotgun, thief.Moving, thief.AkShots,
                               thief.ShotgunShots, dt);
            PoliceWeaponEffects(frame, effects, police.Ak47, police.Shotgun, police.Moving, police.AkShots,
                                police.ShotgunShots, dt);
            effects.Finish();
        }

        void UpdatePoliceAnimation(CharacterVisualState police, double dt)
        {
            // All three idle poses share the original police clock, including
            // the underlying weapon pose while the shield is raised.
            bool idle = police.Active && police.Weapon <= GameRules.Shotgun && !police.Moving;
            policeIdleFrame = policeIdleAnimation.Update(idle, dt);
            WriteAnimationFrame(Control.PoliceIdleFrame, policeIdleFrame);
            WriteAnimationFrame(Control.PoliceRunFrame,
                                policeRunAnimation.Update(police.Unarmed && police.Moving, dt));
            policeAkRunFrame = policeAkRunAnimation.Update(police.Ak47 && police.Moving, dt);
            WriteAnimationFrame(Control.PoliceAkRunFrame, policeAkRunFrame);
            policeShotgunRunFrame = policeShotgunRunAnimation.Update(police.Shotgun && police.Moving, dt);
            WriteAnimationFrame(Control.PoliceShotgunRunFrame, policeShotgunRunFrame);
            WriteAnimationFrame(
                Control.PoliceShieldRunFrame,
                policeShieldRunAnimation.Update(police.Active && police.Shield && police.Moving, dt));
        }

        void UpdateThiefAnimation(CharacterVisualState thief, double dt)
        {
            WriteAnimationFrame(Control.ThiefRunFrame,
                                thiefRunAnimation.Update(thief.Unarmed && thief.Moving, dt));
            WriteAnimationFrame(Control.ThiefIdleFrame,
                                thiefIdleAnimation.Update(thief.Unarmed && !thief.Moving, dt));
            thiefAkIdleFrame = thiefAkAnimation.Update(thief.Ak47 && !thief.Moving, thief.AkShots, dt);
            WriteAnimationFrame(Control.ThiefAkIdleFrame, thiefAkIdleFrame);
            thiefAkRunFrame = thiefAkRunAnimation.Update(thief.Ak47 && thief.Moving, dt);
            WriteAnimationFrame(Control.ThiefAkRunFrame, thiefAkRunFrame);
            thiefShotgunIdleFrame =
                thiefShotgunIdleAnimation.Update(thief.Shotgun && !thief.Moving, thief.ShotgunShots, dt);
            WriteAnimationFrame(Control.ThiefShotgunIdleFrame, thiefShotgunIdleFrame);
            thiefShotgunRunFrame = thiefShotgunRunAnimation.Update(thief.Shotgun && thief.Moving, dt);
            WriteAnimationFrame(Control.ThiefShotgunRunFrame, thiefShotgunRunFrame);
        }

        void WriteAnimationFrame(int controlOffset, int frame)
        {
            Write(ControlBase + (uint)controlOffset, BitConverter.GetBytes(frame));
        }

        void LocateMuzzle(int role, int weapon, uint heading, bool moving, out double x, out double y)
        {
            int frame;
            if (role == 0)
                frame = !moving                       ? policeIdleFrame
                        : weapon == GameRules.Shotgun ? policeShotgunRunFrame
                                                      : policeAkRunFrame;
            else if (weapon == GameRules.Shotgun)
                frame = moving ? thiefShotgunRunFrame : thiefShotgunIdleFrame;
            else
                frame = moving ? thiefAkRunFrame : thiefAkIdleFrame;
            WeaponMuzzle.Offset(role, weapon, (int)heading, moving, frame, out x, out y);
        }

        ShotOrigin LocateShotOrigin(int role, DuelPose pose)
        {
            double x, y;
            LocateMuzzle(role, pose.Weapon, pose.Heading, pose.Moving != 0, out x, out y);
            return new ShotOrigin(pose.X + x, pose.Y + y);
        }
    }
}
