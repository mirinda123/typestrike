using System;
using System.Reflection;
using TypeGameMod;

internal static class PoliceShotgunRuntimeTests
{
    // Exercise the actual publisher method without attaching to any process.
    // This catches role/camera/phase wiring that effect-class tests cannot see.
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(ModSession session, string name, object value)
    {
        typeof(ModSession).GetField(name, Private).SetValue(session, value);
    }

    internal static void Run()
    {
        int cases = 0;
        foreach (int context in new[] { 0, 1, 2 })
            foreach (bool moving in new[] { false, true })
                for (uint heading = 0; heading < 8; heading++)
                    for (int pose = 0; pose < (moving ? 24 : 9); pose++)
                    {
                        var session = new ModSession(null, null);
                        Set(session, "equippedWeapon", 2);
                        Set(session, "policeIdleFrame", moving ? 0 : pose);
                        Set(session, "policeShotgunRunFrame", moving ? pose : 0);
                        uint role = context == 2 ? 1u : 0u;
                        var frame = new GameFrame { Role = role, Online = context != 0, Scene = 3,
                                                    Players = new byte[112] };
                        if (context != 0)
                        {
                            var match = TestDuel.Create(true, (int)role, 450);
                            match.Poses[0] = new DuelPose { Heading = (byte)heading, Weapon = 2,
                                                            Moving = (byte)(moving ? 1 : 0) };
                            Set(session, "networkMatch", match);
                        }
                        var data = new byte[ModSession.SnapshotBytes];
                        BinaryFields.Write(data, SnapshotLayout.PoliceX, 600f);
                        BinaryFields.Write(data, SnapshotLayout.PoliceY, 400f);
                        BinaryFields.Write(data, SnapshotLayout.ThiefX, 900f);
                        BinaryFields.Write(data, SnapshotLayout.ThiefY, 500f);
                        BinaryFields.Write(data, SnapshotLayout.Heading,
                                           context == 0 ? heading : (heading + 3) % 8);
                        BinaryFields.Write(data, SnapshotLayout.MuzzleFlash, 1u);
                        var method = typeof(ModSession).GetMethod("PoliceWeaponEffects", Private);
                        var buffer = new FireEffectBuffer(data);
                        method.Invoke(session,
                                      new object[] { frame, buffer, false, true, moving, 0u, 1u, .008 });
                        buffer.Finish();
                        double mx, my;
                        WeaponMuzzle.Offset(0, GameRules.Shotgun, (int)(heading), moving, pose, out mx,
                                            out my);
                        var expected = new byte[ModSession.SnapshotBytes];
                        var effect = new ShotgunFireEffect();
                        effect.Update(true, 1, 600 + mx, 400 + my, heading, .008);
                        var expectedBuffer = new FireEffectBuffer(expected);
                        if (heading != 0)
                            effect.Append(expectedBuffer, 600 + mx, 400 + my, role == 0 ? 200 : 500,
                                          role == 0 ? 100 : 200);
                        expectedBuffer.Finish();
                        int count = BitConverter.ToInt32(data, SnapshotLayout.ShotgunFxCount);
                        Check(count == BitConverter.ToInt32(expected, SnapshotLayout.ShotgunFxCount),
                              "actual publisher respects rear muzzle occlusion");
                        for (int i = 0; i < count * 20; i++)
                            Check(data[SnapshotLayout.ShotgunFxRects + i] ==
                                      expected[SnapshotLayout.ShotgunFxRects + i],
                                  "local/remote idle/run flash uses the correct gun tip and local camera");
                        Check(BitConverter.ToUInt32(data, SnapshotLayout.MuzzleFlash) ==
                                  (role == 0 ? 0u : 1u),
                              "publisher suppresses only the local police's legacy flash");
                        cases++;
                    }
        Console.WriteLine("PASS police shotgun publisher: " + cases +
                          " local/remote idle/run muzzle, camera, occlusion and legacy-flash cases");
    }

    static void Check(bool value, string reason)
    {
        if (!value)
            throw new Exception(reason);
    }
}
