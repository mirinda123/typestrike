using System;
using TypeGameMod;
internal static partial class BehaviorProbe
{
    // This reference block is extracted from the backed-up 1.18.1 Poll method,
    // before refactoring, and only adapted to access its private fields.
    static void BaselineAnimation(ModSession session, GameFrame frame, InputFrame input, byte[] snapshot,
                                  bool inRound, double dt)
    {
        bool focused = input.Focused;
        var networkMatch = (DuelSession)Get(session, "networkMatch");
        var offlineMatch = (OfflineMatch)Get(session, "offlineMatch");
        var offlineSkill = (RoleSkill)Get(session, "offlineSkill");
        int equippedWeapon = (int)Get(session, "equippedWeapon");
        int policeIdleFrame = 0, policeAkRunFrame = 0, policeShotgunRunFrame = 0, thiefAkIdleFrame = 0,
            thiefAkRunFrame = 0, thiefShotgunIdleFrame = 0, thiefShotgunRunFrame = 0;
        bool typing = inRound && BitConverter.ToUInt32(snapshot, SnapshotLayout.Typing) == 1;

        BinaryFields.Write(snapshot, SnapshotLayout.TypingArtFrame,

                           (uint)((TypingPromptAnimation)Get(session, "typingPromptAnimation"))
                               .Update(typing, focused ? dt : 0));

        bool policeIdle = inRound && focused &&

                          (frame.HasNetworkPlayers && networkMatch != null

                               ? networkMatch.Poses[0].Weapon <= 2

                               : frame.Role == 0 && equippedWeapon <= 2);

        bool policeMoving = frame.HasNetworkPlayers && networkMatch != null

                                ? networkMatch.Poses[0].Moving != 0

                                : input.X != 0 || input.Y != 0;

        policeIdleFrame = ((PoliceIdleAnimation)Get(session, "policeIdleAnimation"))
                              .Update(policeIdle && !policeMoving, dt);

        Call(session, "Write", session.ControlBase + Control.PoliceIdleFrame,
             BitConverter.GetBytes(policeIdleFrame));

        bool policeUnarmed = inRound && focused &&

                             (frame.HasNetworkPlayers && networkMatch != null

                                  ? networkMatch.Poses[0].Weapon == 0 && !networkMatch.Skills[0].Shield

                                  : frame.Role == 0 && equippedWeapon == 0 && !offlineSkill.Shield);

        Call(session, "Write", session.ControlBase + Control.PoliceRunFrame,

             BitConverter.GetBytes(((RunAnimation)Get(session, "policeRunAnimation"))
                                       .Update(policeUnarmed && policeMoving, dt)));

        bool policeAk = inRound && focused &&

                        (frame.HasNetworkPlayers && networkMatch != null

                             ? networkMatch.Poses[0].Weapon == 1 && !networkMatch.Skills[0].Shield

                             : frame.Role == 0 && equippedWeapon == 1 && !offlineSkill.Shield);

        policeAkRunFrame =
            ((RunAnimation)Get(session, "policeAkRunAnimation")).Update(policeAk && policeMoving, dt);

        Call(session, "Write", session.ControlBase + Control.PoliceAkRunFrame,
             BitConverter.GetBytes(policeAkRunFrame));

        bool policeShotgun = inRound && focused &&

                             (frame.HasNetworkPlayers && networkMatch != null

                                  ? networkMatch.Poses[0].Weapon == 2 && !networkMatch.Skills[0].Shield

                                  : frame.Role == 0 && equippedWeapon == 2 && !offlineSkill.Shield);

        policeShotgunRunFrame = ((RunAnimation)Get(session, "policeShotgunRunAnimation"))
                                    .Update(policeShotgun && policeMoving, dt);

        Call(session, "Write", session.ControlBase + Control.PoliceShotgunRunFrame,
             BitConverter.GetBytes(policeShotgunRunFrame));

        bool policeShield = inRound && focused &&

                            (frame.HasNetworkPlayers && networkMatch != null

                                 ? networkMatch.Skills[0].Shield

                                 : frame.Role == 0 && offlineSkill.Shield);

        Call(session, "Write", session.ControlBase + Control.PoliceShieldRunFrame,

             BitConverter.GetBytes(((RunAnimation)Get(session, "policeShieldRunAnimation"))
                                       .Update(policeShield && policeMoving, dt)));

        uint policeShotgunShots = frame.HasNetworkPlayers && networkMatch != null

                                      ? networkMatch.Armories[0].Fired[2]

                                      : frame.Role == 0 ? offlineMatch.Ammo.Fired[2] : 0;

        uint policeShots = frame.HasNetworkPlayers && networkMatch != null

                               ? networkMatch.Armories[0].Fired[1]

                               : frame.Role == 0 ? offlineMatch.Ammo.Fired[1] : 0;

        uint thiefShots = frame.HasNetworkPlayers && networkMatch != null

                              ? networkMatch.Armories[1].Fired[1]

                              : frame.Role == 1 ? offlineMatch.Ammo.Fired[1] : 0;

        bool thiefUnarmed = inRound && focused &&

                            (frame.HasNetworkPlayers && networkMatch != null

                                 ? networkMatch.Poses[1].Weapon == 0

                                 : frame.Role == 1 && equippedWeapon == 0);

        bool thiefMoving = frame.HasNetworkPlayers && networkMatch != null

                               ? networkMatch.Poses[1].Moving != 0

                               : input.X != 0 || input.Y != 0;

        Call(session, "Write", session.ControlBase + Control.ThiefRunFrame,

             BitConverter.GetBytes(
                 ((RunAnimation)Get(session, "thiefRunAnimation")).Update(thiefUnarmed && thiefMoving, dt)));

        Call(session, "Write", session.ControlBase + Control.ThiefIdleFrame,

             BitConverter.GetBytes(((ThiefIdleAnimation)Get(session, "thiefIdleAnimation"))
                                       .Update(thiefUnarmed && !thiefMoving, dt)));

        bool thiefAk = inRound && focused &&

                       (frame.HasNetworkPlayers && networkMatch != null

                            ? networkMatch.Poses[1].Weapon == 1

                            : frame.Role == 1 && equippedWeapon == 1);

        thiefAkIdleFrame = ((ThiefAkAnimation)Get(session, "thiefAkAnimation"))
                               .Update(thiefAk && !thiefMoving, thiefShots, dt);

        Call(session, "Write", session.ControlBase + Control.ThiefAkIdleFrame,
             BitConverter.GetBytes(thiefAkIdleFrame));

        thiefAkRunFrame =
            ((RunAnimation)Get(session, "thiefAkRunAnimation")).Update(thiefAk && thiefMoving, dt);

        Call(session, "Write", session.ControlBase + Control.ThiefAkRunFrame,
             BitConverter.GetBytes(thiefAkRunFrame));

        bool thiefShotgun = inRound && focused &&

                            (frame.HasNetworkPlayers && networkMatch != null

                                 ? networkMatch.Poses[1].Weapon == 2

                                 : frame.Role == 1 && equippedWeapon == 2);

        uint shotgunShots = frame.HasNetworkPlayers && networkMatch != null

                                ? networkMatch.Armories[1].Fired[2]

                                : frame.Role == 1 ? offlineMatch.Ammo.Fired[2] : 0;

        thiefShotgunIdleFrame = ((ThiefShotgunAnimation)Get(session, "thiefShotgunIdleAnimation"))
                                    .Update(thiefShotgun && !thiefMoving, shotgunShots, dt);

        Call(session, "Write", session.ControlBase + Control.ThiefShotgunIdleFrame,
             BitConverter.GetBytes(thiefShotgunIdleFrame));

        thiefShotgunRunFrame =
            ((RunAnimation)Get(session, "thiefShotgunRunAnimation")).Update(thiefShotgun && thiefMoving, dt);

        Call(session, "Write", session.ControlBase + Control.ThiefShotgunRunFrame,
             BitConverter.GetBytes(thiefShotgunRunFrame));

        Set(session, "policeIdleFrame", policeIdleFrame);
        Set(session, "policeAkRunFrame", policeAkRunFrame);
        Set(session, "policeShotgunRunFrame", policeShotgunRunFrame);
        Set(session, "thiefAkIdleFrame", thiefAkIdleFrame);
        Set(session, "thiefAkRunFrame", thiefAkRunFrame);
        Set(session, "thiefShotgunIdleFrame", thiefShotgunIdleFrame);
        Set(session, "thiefShotgunRunFrame", thiefShotgunRunFrame);
        Call(session, "ThiefWeaponEffects", frame, snapshot, thiefAk, thiefShotgun, thiefMoving, thiefShots,
             shotgunShots, dt);

        Call(session, "PoliceWeaponEffects", frame, snapshot, policeAk, policeShotgun, policeMoving,
             policeShots, policeShotgunShots, dt);
    }
}
